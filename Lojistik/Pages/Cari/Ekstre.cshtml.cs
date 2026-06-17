using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Lojistik.Pages.Cari
{
    public class EkstreModel : PageModel
    {
        private readonly AppDbContext _context;
        public EkstreModel(AppDbContext context) => _context = context;

        // ---- Filters (Query) ----
        [BindProperty(SupportsGet = true)] public int musteriId { get; set; }
        [BindProperty(SupportsGet = true)] public string pb { get; set; } = "TL";
        [BindProperty(SupportsGet = true)] public DateTime? d1 { get; set; }   // başlangıç
        [BindProperty(SupportsGet = true)] public DateTime? d2 { get; set; }   // bitiş (dahil)

        [BindProperty(SupportsGet = true)] public bool showClosed { get; set; } = false;
        [BindProperty(SupportsGet = true)] public bool showArsiv { get; set; } = false;
        public HashSet<string> TahsilatEvrakSet { get; private set; } = new();
        [BindProperty(SupportsGet = true)]
        public bool onlyBorclar { get; set; }


        // ---- Header info ----
        public string? MusteriAdi { get; set; }

        // ---- Output rows ----
        public class Row
        {
            public int CariHareketID { get; set; }
            public DateTime Tarih { get; set; }
            public string? IslemTuru { get; set; }
            public string? EvrakNo { get; set; }
            public string? Aciklama { get; set; }
            public decimal Borc { get; set; }     // PB — fatura/sipariş tutarı (müşteri borçlandırıldı)
            public decimal Alacak { get; set; }   // PB — tahsilat/ödeme tutarı (müşteri ödedi)
            public decimal Bakiye { get; set; }   // running PB — pozitif = müşteri borçlu
            public decimal? Kur { get; set; }     // döviz kuru (PB != TL ise)
            public bool Kapandi { get; set; }
            public DateTime? KapanisTarihi { get; set; }
            public int? DevirNo { get; set; }
            public List<PayItem>? Tahsilatlar { get; set; }
            public bool IptalEdilebilir =>
    string.Equals(IslemTuru, "Tahsilat", StringComparison.OrdinalIgnoreCase) && Borc == 0 && Alacak > 0
    || string.Equals(IslemTuru, "Manuel", StringComparison.OrdinalIgnoreCase);

            public string Durum =>
    Kapandi ? "Kapandı"
    : (!string.IsNullOrWhiteSpace(EvrakNo) && Alacak > 0 && Borc > 0) ? "Kısmi"
    : (!string.IsNullOrWhiteSpace(EvrakNo) && Alacak > 0) ? "Açık"
    : "—";
        }

        public List<Row> Items { get; set; } = new();
        public List<Row> ArsivItems { get; set; } = new();

        public class OdenmemisFatura
        {
            public int CariHareketID { get; set; }
            public DateTime Tarih { get; set; }
            public string? IslemTuru { get; set; }
            public string? EvrakNo { get; set; }
            public string? Aciklama { get; set; }
            public decimal Tutar { get; set; }
            public bool IsArsiv { get; set; }
            public decimal OdenenTutar { get; set; }             // kısmi ödemeler varsa
            public decimal KalanTutar => Tutar - OdenenTutar;
        }
        public List<OdenmemisFatura> OdenmemisFaturalar { get; set; } = new();

        public class ArsivGroup
        {
            public int? DevirNo { get; set; }
            public DateTime? KapanmaTarihi { get; set; }
            public bool IsGeriAlinabilir { get; set; }
            public int KayitSayisi => Rows.Count;
            public List<Row> Rows { get; set; } = new();
        }
        public List<ArsivGroup> ArsivGroups { get; set; } = new();

        // Totals (period)
        public decimal ToplamBorc { get; set; }
        public decimal ToplamAlacak { get; set; }
        public decimal DonemNet => ToplamBorc - ToplamAlacak; // pozitif = müşteri net borçlu

        // Opening/Closing
        public decimal AcilisBakiye { get; set; }   // d1 öncesi net (PB)
        public decimal KapanisBakiye { get; set; }  // d2 sonu net (PB)

        public class DetayRow
        {
            public DateTime Tarih { get; set; }
            public decimal Tutar { get; set; }
            public string Kaynak { get; set; } = "";
            public string? Aciklama { get; set; }
        }
        public Dictionary<string, List<DetayRow>> KapananDetayByEvrak { get; set; } = new();

        // Köşeli parantezli etiketleri ve "Eşleşmeyen Evrak" varyasyonlarını siler
        private static string CleanText(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var t = s;
            t = Regex.Replace(t, @"\[\s*Eşleşmeyen[^\]]*\]", "", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"Eşleşmeyen\s*Evrak(No)?", "", RegexOptions.IgnoreCase);
            return t.Trim();
        }

        // EvrakNo'yu normalize et (Trim + iç boşlukları sıkıştırma)
        private static string? NormalizeDoc(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim();
            t = Regex.Replace(t, @"\s+", " "); // birden çok boşluğu teke indir
            return t;
        }


        public async Task<IActionResult> OnPostIptalAsync(int id, int musteriId, string pb, DateTime? d1, DateTime? d2)
        {
            var firmaId = User.GetFirmaId();

            // Silinecek hareketi doğrula (sadece Tahsilat ve Yonu=0 olanlar)
            var h = await _context.CariHareketler
                .AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.CariHareketID == id)
                .Select(x => new
                {
                    x.CariHareketID,
                    x.IslemTuru,
                    x.Yonu,
                    x.IlgiliSiparisID,
                    x.IlgiliSevkiyatID
                })
                .FirstOrDefaultAsync();

            if (h is null)
            {
                TempData["StatusMessage"] = "Hareket bulunamadı.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }
            // İzin verilen türler:
            bool isTahsilat = string.Equals(h.IslemTuru, "Tahsilat", StringComparison.OrdinalIgnoreCase) && h.Yonu == 0;
            bool isManuel = string.Equals(h.IslemTuru, "Manuel", StringComparison.OrdinalIgnoreCase);

            // İlişkili sipariş/sevkiyatlı kayıtları asla silmeyelim
            bool hasRelation = h.IlgiliSiparisID != null || h.IlgiliSevkiyatID != null;

            if (!(isTahsilat || isManuel) || hasRelation)
            {
                TempData["StatusMessage"] = hasRelation
                    ? "İlişkili hareketler silinemez."
                    : "Bu hareket silinemez.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            try
            {
                // Hard delete:
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM dbo.CariHareketler WHERE FirmaID={0} AND CariHareketID={1}", firmaId, id);
                TempData["StatusMessage"] = "Hareket silindi.";
            }
            catch (Exception ex)
            {
                TempData["StatusMessage"] = "Silme sırasında hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new { musteriId, pb, d1, d2 });
        }


        // ...

        // GET: CSV
        public async Task<IActionResult> OnGetExportExcelAsync(
            int musteriId, string pb, DateTime? d1, DateTime? d2, bool showClosed = false)
        {
            var firmaId = User.GetFirmaId();
            if (musteriId <= 0 || string.IsNullOrWhiteSpace(pb)) return RedirectToPage("./Index");

            pb = pb.Trim().ToUpperInvariant();
            var start = d1?.Date ?? new DateTime(DateTime.Today.Year, 1, 1);
            var end = (d2?.Date ?? DateTime.Today);
            if (end < start) end = start;

            var musteriAdi = await _context.Musteriler.AsNoTracking()
                .Where(m => m.MusteriID == musteriId && m.FirmaID == firmaId)
                .Select(m => m.MusteriAdi)
                .FirstOrDefaultAsync() ?? $"#{musteriId}";

            var acilis = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb && ch.Tarih < start && !ch.IsArsiv)
                .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
                .SumAsync();

            // --- EvrakNo bazlı sözlükler ---
            // Alacak (fatura/manuel alacak)
            var alacakByEvrak = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && !x.IsArsiv && ((x.IslemTuru == "Sipariş" && x.Yonu == 1) || (x.IslemTuru == "Manuel" && x.Yonu == 1)))
                .GroupBy(x => x.EvrakNo!)
                .Select(g => new { EvrakNo = g.Key, Tutar = g.Sum(z => z.Tutar) })
                .ToDictionaryAsync(k => k.EvrakNo, v => v.Tutar);

            // Tahsilat satırları (liste + toplam + kapanma tarihi)
            var tahsilatList = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && !x.IsArsiv && ((x.IslemTuru == "Tahsilat" && x.Yonu == 0) || (x.IslemTuru == "Manuel" && x.Yonu == 0)))
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { EvrakNo = x.EvrakNo!, x.Tarih, x.Tutar })
                .ToListAsync();

            var tahsilatSumByEvrak = tahsilatList
                .GroupBy(g => g.EvrakNo)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            var kapanmaTarihiByEvrak = tahsilatList
                .GroupBy(g => g.EvrakNo)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(z => z.Tarih) // son tahsilat tarihi
                );

            // Ekrandaki görünür mantıkla satırlar (her hareket kendi satırı — netleştirme yok)
            var rows = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && (
                              (x.IslemTuru == "Sipariş" && x.Yonu == 1) ||
                              (x.IslemTuru == "Manuel" && (x.Yonu == 1 || x.Yonu == 0)) ||
                              (x.IslemTuru == "Tahsilat" && x.Yonu == 0) ||
                              (x.IslemTuru == "Sefer Alacak" && x.Yonu == 1) ||
                              x.IslemTuru == "DevirBakiye"
                            ) && !x.IsArsiv)
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Yonu, x.Tutar })
                .ToListAsync();

            var tr = new System.Globalization.CultureInfo("tr-TR");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine($"Müşteri;{musteriAdi};PB;{pb};Filtre;{(showClosed ? "Kapanan" : "Açık/Kısmi")}");

            // showClosed ise "Kapanma" kolonu dahil
            sb.AppendLine(showClosed
                ? "Tarih;İşlem;Evrak No;Açıklama;Kapanma;Borç;Alacak;Bakiye"
                : "Tarih;İşlem;Evrak No;Açıklama;Borç;Alacak;Bakiye");

            decimal bakiye = acilis;

            foreach (var h in rows)
            {
                decimal borc = 0m, alacak = 0m;
                // Yonu=1 (Sipariş/Manuel/Sefer Alacak/DevirBakiye) → Borç; Yonu=0 → Alacak (ödeme)
                bool isBorcSatir = (h.IslemTuru == "Sipariş" && h.Yonu == 1)
                                || (h.IslemTuru == "Manuel" && h.Yonu == 1)
                                || (h.IslemTuru == "Sefer Alacak" && h.Yonu == 1)
                                || (h.IslemTuru == "DevirBakiye" && h.Yonu == 1);
                if (isBorcSatir) borc = h.Tutar; else alacak = h.Tutar;

                // Kapanma durumu: bu satırın EvrakNo'su, ilgili faturanın toplam tutarına eşit/üstünde tahsil edilmişse "kapandı" sayılır
                var hasEvrak = !string.IsNullOrEmpty(h.EvrakNo);
                bool isClosed = hasEvrak
                                && alacakByEvrak.TryGetValue(h.EvrakNo!, out var alc)
                                && tahsilatSumByEvrak.TryGetValue(h.EvrakNo!, out var tah)
                                && tah >= alc;

                if (!showClosed && isClosed) continue;
                if (showClosed && !isClosed) continue;

                bakiye += (borc - alacak);

                var kapanmaStr = (showClosed && isClosed && kapanmaTarihiByEvrak.TryGetValue(h.EvrakNo!, out var kt))
                    ? kt.ToString("dd.MM.yyyy", tr) : "";

                var cols = new List<string>
        {
            h.Tarih.ToString("dd.MM.yyyy", tr),
            h.IslemTuru ?? "",
            string.IsNullOrWhiteSpace(h.EvrakNo) ? "" : h.EvrakNo!,
            (h.Aciklama ?? "").Replace(";", ",")
        };
                if (showClosed) cols.Add(kapanmaStr);
                cols.Add(borc > 0 ? borc.ToString("N2", tr) : "");
                cols.Add(alacak > 0 ? alacak.ToString("N2", tr) : "");
                cols.Add(bakiye.ToString("N2", tr));

                sb.AppendLine(string.Join(";", cols));
            }

            var utf8 = System.Text.Encoding.UTF8;
            var bytes = utf8.GetPreamble().Concat(utf8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"Ekstre_{musteriAdi}_{pb}_{(showClosed ? "KAPANAN" : "ACIK")}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnGetExportPdfAsync(
            int musteriId, string pb, DateTime? d1, DateTime? d2, bool showClosed = false)
        {
            var firmaId = User.GetFirmaId();
            if (musteriId <= 0 || string.IsNullOrWhiteSpace(pb)) return RedirectToPage("./Index");

            pb = pb.Trim().ToUpperInvariant();
            var start = d1?.Date ?? new DateTime(DateTime.Today.Year, 1, 1);
            var end = (d2?.Date ?? DateTime.Today);
            if (end < start) end = start;

            var musteriAdi = await _context.Musteriler.AsNoTracking()
                .Where(m => m.MusteriID == musteriId && m.FirmaID == firmaId)
                .Select(m => m.MusteriAdi)
                .FirstOrDefaultAsync() ?? $"#{musteriId}";

            var acilis = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb && ch.Tarih < start && !ch.IsArsiv)
                .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
                .SumAsync();

            // --- EvrakNo bazlı sözlükler (alacak, tahsilat, kapanma) ---
            var alacakByEvrak = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && !x.IsArsiv && ((x.IslemTuru == "Sipariş" && x.Yonu == 1) || (x.IslemTuru == "Manuel" && x.Yonu == 1)))
                .GroupBy(x => x.EvrakNo!)
                .Select(g => new { EvrakNo = g.Key, Tutar = g.Sum(z => z.Tutar) })
                .ToDictionaryAsync(k => k.EvrakNo, v => v.Tutar);

            var tahsilatList = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && !x.IsArsiv && ((x.IslemTuru == "Tahsilat" && x.Yonu == 0) || (x.IslemTuru == "Manuel" && x.Yonu == 0)))
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { EvrakNo = x.EvrakNo!, x.Tarih, x.Tutar })
                .ToListAsync();

            var tahsilatSumByEvrak = tahsilatList.GroupBy(g => g.EvrakNo).ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));
            var kapanmaTarihiByEvrak = tahsilatList.GroupBy(g => g.EvrakNo).ToDictionary(g => g.Key, g => g.Max(z => z.Tarih));

            var rows = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && (
                              (x.IslemTuru == "Sipariş" && x.Yonu == 1) ||
                              (x.IslemTuru == "Manuel" && (x.Yonu == 1 || x.Yonu == 0)) ||
                              (x.IslemTuru == "Tahsilat" && x.Yonu == 0) ||
                              (x.IslemTuru == "Sefer Alacak" && x.Yonu == 1) ||
                              x.IslemTuru == "DevirBakiye"
                            ) && !x.IsArsiv)
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Yonu, x.Tutar })
                .ToListAsync();

            // Listeyi, showClosed ise "Kapanma" alanıyla kur
            var list = new List<(string Tarih, string Islem, string EvrakNo, string Aciklama, string Kapanma, string Borc, string Alacak, string Bakiye)>();
            decimal bakiye = acilis;

            foreach (var h in rows)
            {
                decimal borc = 0m, alacak = 0m;
                // Yonu=1 (Sipariş/Manuel/Sefer Alacak/DevirBakiye) → Borç; Yonu=0 → Alacak (ödeme)
                bool isBorcSatir = (h.IslemTuru == "Sipariş" && h.Yonu == 1)
                                || (h.IslemTuru == "Manuel" && h.Yonu == 1)
                                || (h.IslemTuru == "Sefer Alacak" && h.Yonu == 1)
                                || (h.IslemTuru == "DevirBakiye" && h.Yonu == 1);
                if (isBorcSatir) borc = h.Tutar; else alacak = h.Tutar;

                // Kapanma durumu: bu satırın EvrakNo'su, ilgili faturanın toplam tutarına eşit/üstünde tahsil edilmişse "kapandı" sayılır
                var hasEvrak = !string.IsNullOrEmpty(h.EvrakNo);
                bool isClosed = hasEvrak
                                && alacakByEvrak.TryGetValue(h.EvrakNo!, out var alc)
                                && tahsilatSumByEvrak.TryGetValue(h.EvrakNo!, out var tah)
                                && tah >= alc;

                if (!showClosed && isClosed) continue;
                if (showClosed && !isClosed) continue;

                bakiye += (borc - alacak);

                var kapanmaStr = (showClosed && isClosed && kapanmaTarihiByEvrak.TryGetValue(h.EvrakNo!, out var kt))
                    ? kt.ToString("dd.MM.yyyy") : "";

                list.Add((
                    h.Tarih.ToString("dd.MM.yyyy"),
                    h.IslemTuru ?? "",
                    string.IsNullOrWhiteSpace(h.EvrakNo) ? "" : h.EvrakNo!,
                    h.Aciklama ?? "",
                    kapanmaStr,
                    borc > 0 ? borc.ToString("N2") : "",
                    alacak > 0 ? alacak.ToString("N2") : "",
                    bakiye.ToString("N2")
                ));
            }

            var kapanis = bakiye;

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8)); // küçültülmüş

                    page.Header().Text($"Cari Ekstre - {musteriAdi} / {pb}  ({(showClosed ? "Kapanan" : "Açık/Kısmi")})")
                                 .SemiBold().FontSize(10);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(50);  // Tarih
                            c.RelativeColumn(14);  // İşlem
                            c.RelativeColumn(22);  // Evrak
                            c.RelativeColumn(58);  // Açıklama
                            if (showClosed) c.ConstantColumn(50); // Kapanma
                            c.ConstantColumn(50);  // Borç
                            c.ConstantColumn(50);  // Alacak
                            c.ConstantColumn(60);  // Bakiye
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(H).Text("Tarih");
                            h.Cell().Element(H).Text("İşlem");
                            h.Cell().Element(H).Text("Evrak");
                            h.Cell().Element(H).Text("Açıklama");
                            if (showClosed) h.Cell().Element(H).Text("Kapanma");
                            h.Cell().Element(H).AlignRight().Text(t => { t.Line("Borç"); t.Line($"({pb})"); });
                            h.Cell().Element(H).AlignRight().Text(t => { t.Line("Alacak"); t.Line($"({pb})"); });
                            h.Cell().Element(H).AlignRight().Text(t => { t.Line("Bakiye"); t.Line($"({pb})"); });

                            static QuestPDF.Infrastructure.IContainer H(QuestPDF.Infrastructure.IContainer c) =>
                                c.DefaultTextStyle(x => x.SemiBold())
                                 .PaddingVertical(3).PaddingHorizontal(3)
                                 .BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Medium);
                        });

                        foreach (var r in list)
                        {
                            table.Cell().Element(C).Text(r.Tarih);
                            table.Cell().Element(C).Text(r.Islem);
                            table.Cell().Element(C).Text(r.EvrakNo);
                            table.Cell().Element(C).Text(r.Aciklama);
                            if (showClosed) table.Cell().Element(C).Text(r.Kapanma);
                            table.Cell().Element(C).AlignRight().Text(r.Borc);
                            table.Cell().Element(C).AlignRight().Text(r.Alacak);
                            table.Cell().Element(C).AlignRight().Text(r.Bakiye);

                            static QuestPDF.Infrastructure.IContainer C(QuestPDF.Infrastructure.IContainer c) =>
                                c.PaddingVertical(2).PaddingHorizontal(3);
                        }

                        table.Cell().ColumnSpan(showClosed ? 5u : 4u).Element(F).AlignRight().Text("Kapanış");
                        table.Cell().Element(F).AlignRight().Text("");
                        table.Cell().Element(F).AlignRight().Text("");
                        table.Cell().Element(F).AlignRight().Text(kapanis.ToString("N2"));

                        static QuestPDF.Infrastructure.IContainer F(QuestPDF.Infrastructure.IContainer c) =>
                            c.PaddingVertical(3).PaddingHorizontal(3)
                             .BorderTop(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Medium)
                             .DefaultTextStyle(x => x.SemiBold());
                    });

                    page.Footer().DefaultTextStyle(x => x.FontSize(6))
                                 .AlignRight()
                                 .Text($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });

            var pdf = doc.GeneratePdf();
            var fileName = $"Ekstre_{musteriAdi}_{pb}_{(showClosed ? "KAPANAN" : "ACIK")}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (musteriId <= 0 || string.IsNullOrWhiteSpace(pb)) return RedirectToPage("./Index");
            pb = pb.Trim().ToUpperInvariant();

            var start = d1?.Date ?? new DateTime(DateTime.Today.Year, 1, 1);
            var end = (d2?.Date ?? DateTime.Today).Date;
            if (end < start) end = start;

            // Müşteri adı
            MusteriAdi = await _context.Musteriler.AsNoTracking()
                .Where(m => m.MusteriID == musteriId && m.FirmaID == firmaId)
                .Select(m => m.MusteriAdi)
                .FirstOrDefaultAsync() ?? $"#{musteriId}";

            // Açılış bakiye
            AcilisBakiye = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb && ch.Tarih < start && !ch.IsArsiv)
                .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
                .SumAsync();

            // TÜM hareketleri (belge ve tahsilatlar) tek tek çek
            var rows = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId
                         && x.MusteriID == musteriId
                         && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && (
     (x.IslemTuru == "Sipariş" && x.Yonu == 1) ||
     (x.IslemTuru == "Manuel" && (x.Yonu == 1 || x.Yonu == 0)) ||
     (x.IslemTuru == "Tahsilat" && x.Yonu == 0) ||
     (x.IslemTuru == "Sefer Alacak" && x.Yonu == 1) ||
     x.IslemTuru == "DevirBakiye"
   ) && !x.IsArsiv)
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { x.CariHareketID, x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Yonu, x.Tutar, x.Kur })
                .ToListAsync();

            // Satır satır bakiye
            Items.Clear();
            decimal bakiye = AcilisBakiye;

            foreach (var h in rows)
            {
                decimal borc = 0m, alacak = 0m;
                // Yonu=1 (fatura/sipariş) → Borç kolonuna; Yonu=0 (tahsilat/ödeme) → Alacak kolonuna
                if ((h.IslemTuru == "Sipariş" && h.Yonu == 1) || (h.IslemTuru == "Manuel" && h.Yonu == 1) || (h.IslemTuru == "Sefer Alacak" && h.Yonu == 1) || (h.IslemTuru == "DevirBakiye" && h.Yonu == 1))
                    borc = h.Tutar;   // müşteri borçlandırıldı
                else
                    alacak = h.Tutar; // müşteri ödedi

                bakiye += (borc - alacak); // pozitif bakiye = müşteri borçlu

                Items.Add(new Row
                {
                    CariHareketID = h.CariHareketID,
                    Tarih = h.Tarih,
                    IslemTuru = h.IslemTuru,
                    EvrakNo = NormalizeDoc(h.EvrakNo),
                    Aciklama = CleanText(h.Aciklama),
                    Borc = borc,
                    Alacak = alacak,
                    Bakiye = bakiye,
                    Kur = h.Kur,
                    Kapandi = false,
                    KapanisTarihi = null,
                    Tahsilatlar = null
                });
  
            }
            if (onlyBorclar)
            {
                Items = Items
                    .Where(x => !string.Equals(x.IslemTuru, "Tahsilat", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            ToplamBorc = Items.Sum(x => x.Borc);
            ToplamAlacak = Items.Sum(x => x.Alacak);
            KapanisBakiye = bakiye;

            d1 = start; d2 = end;

            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();
            ViewData["Yetki2"] = yetki2;

            TahsilatEvrakSet = rows
    .Where(x => string.Equals(x.IslemTuru, "Tahsilat", StringComparison.OrdinalIgnoreCase)
             && !string.IsNullOrWhiteSpace(x.EvrakNo))
    .Select(x => x.EvrakNo!.Trim().ToUpperInvariant())
    .ToHashSet();

            // Ödenmemiş / kısmi ödenmiş faturalar (aktif + arşiv birlikte) — her zaman çekilir
            {
                // Tüm alacak faturalar (aktif + arşiv)
                var tumFaturalar = await _context.CariHareketler.AsNoTracking()
                    .Where(x => x.FirmaID == firmaId
                             && x.MusteriID == musteriId
                             && x.ParaBirimi == pb
                             && x.Yonu == 1
                             && x.EvrakNo != null
                             && (x.IslemTuru == "Sipariş" || x.IslemTuru == "Manuel" || x.IslemTuru == "Sefer Alacak"))
                    .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                    .Select(x => new { x.CariHareketID, x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Tutar, x.IsArsiv })
                    .ToListAsync();

                // Tüm tahsilatlar (aktif + arşiv) — EvrakNo bazında topla
                var tahsilatSumByEvrak = await _context.CariHareketler.AsNoTracking()
                    .Where(x => x.FirmaID == firmaId
                             && x.MusteriID == musteriId
                             && x.ParaBirimi == pb
                             && x.Yonu == 0
                             && x.EvrakNo != null
                             && (x.IslemTuru == "Tahsilat" || x.IslemTuru == "Manuel"))
                    .GroupBy(x => x.EvrakNo!)
                    .Select(g => new { EvrakNo = g.Key, Toplam = g.Sum(z => z.Tutar) })
                    .ToDictionaryAsync(k => k.EvrakNo, v => v.Toplam);

                // Tam ödenmemiş veya kısmi ödenmiş olanları filtrele
                foreach (var f in tumFaturalar)
                {
                    tahsilatSumByEvrak.TryGetValue(f.EvrakNo!, out var odenen);
                    if (odenen >= f.Tutar) continue; // tam ödenmiş → atla

                    OdenmemisFaturalar.Add(new OdenmemisFatura
                    {
                        CariHareketID = f.CariHareketID,
                        Tarih         = f.Tarih,
                        IslemTuru     = f.IslemTuru,
                        EvrakNo       = NormalizeDoc(f.EvrakNo),
                        Aciklama      = CleanText(f.Aciklama),
                        Tutar         = f.Tutar,
                        IsArsiv       = f.IsArsiv,
                        OdenenTutar   = odenen
                    });
                }
            }

            // Arşivlenmiş hareketler (devir alınmış) – DevirNo gruplarına göre (her zaman yükle)
            {
                var arsivRows = await _context.CariHareketler.AsNoTracking()
                    .Where(x => x.FirmaID == firmaId
                             && x.MusteriID == musteriId
                             && x.ParaBirimi == pb
                             && x.IsArsiv)
                    .OrderBy(x => x.DevirNo).ThenBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                    .Select(x => new { x.CariHareketID, x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Yonu, x.Tutar, x.DevirNo })
                    .ToListAsync();

                // DevirBakiye kayıtlarından kapanma tarihlerini çek
                var devirBakiyeler = await _context.CariHareketler.AsNoTracking()
                    .Where(x => x.FirmaID == firmaId
                             && x.MusteriID == musteriId
                             && x.IslemTuru == "DevirBakiye"
                             && x.DevirNo != null)
                    .Select(x => new { x.DevirNo, x.DevirKapanmaTarihi })
                    .ToListAsync();
                var ktDict = devirBakiyeler
                    .Where(x => x.DevirNo.HasValue)
                    .ToDictionary(x => x.DevirNo!.Value, x => x.DevirKapanmaTarihi);

                decimal arsivBakiye = 0m;
                foreach (var grp in arsivRows.GroupBy(x => x.DevirNo).OrderBy(g => g.Key))
                {
                    ktDict.TryGetValue(grp.Key ?? 0, out var kt);
                    var ag = new ArsivGroup
                    {
                        DevirNo          = grp.Key,
                        KapanmaTarihi    = kt,
                        IsGeriAlinabilir = grp.Key.HasValue  // tüm devir grupları geri alınabilir (cascade)
                    };
                    foreach (var h in grp)
                    {
                        decimal borc = 0m, alacak = 0m;
                        if (h.Yonu == 1) borc = h.Tutar; else alacak = h.Tutar;
                        arsivBakiye += (borc - alacak);
                        var row = new Row
                        {
                            CariHareketID = h.CariHareketID,
                            Tarih         = h.Tarih,
                            IslemTuru     = h.IslemTuru,
                            EvrakNo       = NormalizeDoc(h.EvrakNo),
                            Aciklama      = CleanText(h.Aciklama),
                            Borc          = borc,
                            Alacak        = alacak,
                            Bakiye        = arsivBakiye,
                            DevirNo       = h.DevirNo
                        };
                        ag.Rows.Add(row);
                        ArsivItems.Add(row);
                    }
                    ArsivGroups.Add(ag);
                }
            }

            return Page();
        }

        // ── Tahsilat Yap (Modal Form) ─────────────────────────────────────────
        public async Task<IActionResult> OnPostTahsilatYapAsync(decimal tutar, string? aciklama, DateTime? tahsilatTarihi, int musteriId, string pb, DateTime? d1, DateTime? d2)
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            // Yetki kontrol
            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();

            if (yetki2 != 2)
            {
                TempData["StatusMessage"] = "Yetkiniz yok.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            if (tutar <= 0)
            {
                TempData["StatusMessage"] = "Tutar sıfırdan büyük olmalıdır.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            var musteriAit = await _context.Musteriler
                .AnyAsync(m => m.FirmaID == firmaId && m.MusteriID == musteriId);
            if (!musteriAit) return Forbid();

            var tahsilat = new CariHareket
            {
                FirmaID = firmaId,
                MusteriID = musteriId,
                ParaBirimi = pb.Trim().ToUpperInvariant(),
                Tarih = tahsilatTarihi?.Date ?? DateTime.Today,
                IslemTuru = "Tahsilat",
                Aciklama = aciklama?.Trim(),
                Yonu = 0,
                Tutar = tutar,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now
            };

            _context.CariHareketler.Add(tahsilat);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"Tahsilat eklendi: {tutar:N2} {pb}";
            return RedirectToPage(new { musteriId, pb, d1, d2 });
        }

        // ── Otomatik Tahsil (Sipariş Satırından) ────────────────────────────────
        public async Task<IActionResult> OnPostTahsilEtAsync(int id, int musteriId, string pb, DateTime? d1, DateTime? d2)
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            // Yetki kontrol (UI'da zaten var ama server-side da kalsın)
            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();

            if (yetki2 != 2)
            {
                TempData["StatusMessage"] = "Yetkiniz yok.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            // Kaynak satır: alacak satırı (Sipariş / Manuel / Sefer Alacak) ve Yonu=1
            var kaynak = await _context.CariHareketler
                .AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.CariHareketID == id)
                .Select(x => new
                {
                    x.CariHareketID,
                    x.MusteriID,
                    x.ParaBirimi,
                    x.Tarih,
                    x.IslemTuru,
                    x.EvrakNo,
                    x.Tutar,
                    x.Yonu,
                    x.IlgiliSiparisID,
                    x.IlgiliSevkiyatID
                })
                .FirstOrDefaultAsync();

            if (kaynak == null)
            {
                TempData["StatusMessage"] = "Kayıt bulunamadı.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            bool uygunAlacak =
                kaynak.Yonu == 1 &&
                kaynak.Tutar > 0 &&
                !string.IsNullOrWhiteSpace(kaynak.EvrakNo) &&
                (kaynak.IslemTuru == "Sipariş" || kaynak.IslemTuru == "Manuel" || kaynak.IslemTuru == "Sefer Alacak");

            if (!uygunAlacak)
            {
                TempData["StatusMessage"] = "Bu satıra tahsilat yapılamaz.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            // Aynı evrak için zaten tahsilat var mı?
            var evrakKey = kaynak.EvrakNo!.Trim().ToUpperInvariant();

            var varMi = await _context.CariHareketler.AnyAsync(x =>
                x.FirmaID == firmaId &&
                x.Yonu == 0 &&
                x.IslemTuru == "Tahsilat" &&
                x.EvrakNo != null &&
                x.EvrakNo.Trim().ToUpper() == evrakKey &&
                !x.IsArsiv);

            if (varMi)
            {
                TempData["StatusMessage"] = "Bu evrak zaten tahsil edilmiş.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            // Tahsilat kaydı ekle
            // NOT: Aşağıdaki entity tipi sende neyse (CariHareket / CariHareketler) ona göre düzelt.
            var yeni = new CariHareket
            {
                FirmaID = firmaId,
                MusteriID = kaynak.MusteriID,
                ParaBirimi = kaynak.ParaBirimi,
                Tarih = DateTime.Today,
                IslemTuru = "Tahsilat",
                EvrakNo = kaynak.EvrakNo,              // set mantığınla uyumlu
                Aciklama = $"Tahsilat: {kaynak.EvrakNo}",
                Yonu = 0,
                Tutar = kaynak.Tutar
                // CreatedByKullaniciID / CreatedAt alanların varsa burada doldur
            };

            _context.CariHareketler.Add(yeni);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Tahsilat eklendi.";
            return RedirectToPage(new { musteriId, pb, d1, d2 });
        }




        // ── Ödendi Yap ────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostOdendiyapAsync(int id, int musteriId, string pb, DateTime? d1, DateTime? d2)
        {
            var firmaId = User.GetFirmaId();
            var userId  = User.GetUserId();

            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();
            if (yetki2 != 2) { TempData["StatusMessage"] = "Yetkiniz yok."; return RedirectToPage(new { musteriId, pb, d1, d2 }); }

            // Fatura kaydını doğrula
            var fatura = await _context.CariHareketler
                .Where(x => x.FirmaID == firmaId && x.CariHareketID == id && x.Yonu == 1 && x.EvrakNo != null)
                .Select(x => new { x.CariHareketID, x.MusteriID, x.ParaBirimi, x.EvrakNo, x.Tutar, x.IslemTuru })
                .FirstOrDefaultAsync();

            if (fatura == null)
            {
                TempData["StatusMessage"] = "Fatura kaydı bulunamadı.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            // Mevcut tahsilatları topla (aktif + arşiv)
            var mevcutOdeme = await _context.CariHareketler
                .Where(x => x.FirmaID == firmaId
                         && x.MusteriID == fatura.MusteriID
                         && x.EvrakNo == fatura.EvrakNo
                         && x.Yonu == 0
                         && (x.IslemTuru == "Tahsilat" || x.IslemTuru == "Manuel"))
                .SumAsync(x => (decimal?)x.Tutar) ?? 0m;

            var kalan = fatura.Tutar - mevcutOdeme;
            if (kalan <= 0)
            {
                TempData["StatusMessage"] = $"{fatura.EvrakNo} zaten tam ödenmiş.";
                return RedirectToPage(new { musteriId, pb, d1, d2 });
            }

            _context.CariHareketler.Add(new CariHareket
            {
                FirmaID              = firmaId,
                MusteriID            = fatura.MusteriID,
                ParaBirimi           = fatura.ParaBirimi,
                Tarih                = DateTime.Today,
                IslemTuru            = "Tahsilat",
                EvrakNo              = fatura.EvrakNo,
                Aciklama             = $"Tahsilat: {fatura.EvrakNo}",
                Yonu                 = 0,
                Tutar                = kalan,
                CreatedByKullaniciID = userId,
                CreatedAt            = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["StatusMessage"]    = $"{fatura.EvrakNo} faturası tahsil edildi ({kalan:N2} {fatura.ParaBirimi}).";
            TempData["ReopenOdenmemis"] = true;   // modal yeniden açılsın
            return RedirectToPage(new { musteriId, pb, d1, d2 });
        }

        // ── Devir Geri Al ─────────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostDevirGeriAlAsync(int devirNo, int musteriId, string pb)
        {
            var firmaId = User.GetFirmaId();
            var userId  = User.GetUserId();

            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();
            if (yetki2 != 2) return Forbid();

            // Cascade: seçilen devir dahil daha yeni tüm devirleri geri al (büyükten küçüğe)
            var devirNosToRevert = await _context.CariHareketler
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId
                          && ch.DevirNo.HasValue && ch.DevirNo >= devirNo)
                .Select(ch => ch.DevirNo!.Value)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            if (!devirNosToRevert.Any())
            {
                TempData["StatusMessage"] = $"Devir #{devirNo} bulunamadı.";
                return RedirectToPage(new { musteriId, pb, showArsiv = true });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int toplamGeriAlinan = 0;
                foreach (var dn in devirNosToRevert)
                {
                    // 1. Arşivlenmiş kayıtları aktife al (tüm para birimleri)
                    var geri = await _context.Database.ExecuteSqlRawAsync(@"
                        UPDATE dbo.CariHareketler
                        SET    IsArsiv = 0, DevirNo = NULL
                        WHERE  FirmaID   = {0}
                          AND  MusteriID = {1}
                          AND  IsArsiv   = 1
                          AND  DevirNo   = {2}
                          AND  IslemTuru <> 'DevirBakiye'",
                        firmaId, musteriId, dn);
                    toplamGeriAlinan += geri;

                    // 2. DevirBakiye kaydını sil
                    await _context.Database.ExecuteSqlRawAsync(@"
                        DELETE FROM dbo.CariHareketler
                        WHERE  FirmaID   = {0}
                          AND  MusteriID = {1}
                          AND  DevirNo   = {2}
                          AND  IslemTuru = 'DevirBakiye'",
                        firmaId, musteriId, dn);
                }

                await transaction.CommitAsync();
                var label = devirNosToRevert.Count > 1
                    ? $"#{string.Join(", #", devirNosToRevert)} (cascade)"
                    : $"#{devirNo}";
                TempData["StatusMessage"] = $"Devir {label} geri alındı. {toplamGeriAlinan} kayıt yeniden aktive edildi.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["StatusMessage"] = "Hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new { musteriId, pb });
        }

        public class PayItem
        {
            public int CariHareketID { get; set; }
            public DateTime Tarih { get; set; }
            public decimal Tutar { get; set; }         // (+) PB
            public string Kaynak { get; set; } = "";   // Tahsilat / Manuel
            public string? Aciklama { get; set; }
        }
    }
}
