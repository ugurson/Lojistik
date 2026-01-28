using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;


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
            public decimal Borc { get; set; }     // PB
            public decimal Alacak { get; set; }   // PB
            public decimal Bakiye { get; set; }   // running PB
            public bool Kapandi { get; set; }
            public DateTime? KapanisTarihi { get; set; }
            public List<PayItem>? Tahsilatlar { get; set; }
            public bool IptalEdilebilir =>
    string.Equals(IslemTuru, "Tahsilat", StringComparison.OrdinalIgnoreCase) && Alacak == 0 && Borc > 0
    || string.Equals(IslemTuru, "Manuel", StringComparison.OrdinalIgnoreCase);  // <-- eklendi

            public string Durum =>
    Kapandi ? "Kapandı"
    : (!string.IsNullOrWhiteSpace(EvrakNo) && Alacak > 0 && Borc > 0) ? "Kısmi"
    : (!string.IsNullOrWhiteSpace(EvrakNo) && Alacak > 0) ? "Açık"
    : "—";
        }

        public List<Row> Items { get; set; } = new();

        // Totals (period)
        public decimal ToplamBorc { get; set; }
        public decimal ToplamAlacak { get; set; }
        public decimal DonemNet => ToplamAlacak - ToplamBorc;

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
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb && ch.Tarih < start)
                .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
                .SumAsync();

            // --- EvrakNo bazlı sözlükler ---
            // Alacak (fatura/manuel alacak)
            var alacakByEvrak = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && ((x.IslemTuru == "Sipariş" && x.Yonu == 1) || (x.IslemTuru == "Manuel" && x.Yonu == 1)))
                .GroupBy(x => x.EvrakNo!)
                .Select(g => new { EvrakNo = g.Key, Tutar = g.Sum(z => z.Tutar) })
                .ToDictionaryAsync(k => k.EvrakNo, v => v.Tutar);

            // Tahsilat satırları (liste + toplam + kapanma tarihi)
            var tahsilatList = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && ((x.IslemTuru == "Tahsilat" && x.Yonu == 0) || (x.IslemTuru == "Manuel" && x.Yonu == 0)))
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

            // Ekrandaki görünür mantıkla satırlar
            var rows = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && (
                              (x.IslemTuru == "Sipariş" && x.Yonu == 1 && x.EvrakNo != null) ||
                              (x.IslemTuru == "Manuel" && x.Yonu == 1) ||
                              ((x.IslemTuru == "Tahsilat" || x.IslemTuru == "Manuel") && x.Yonu == 0 && x.EvrakNo == null)
                            ))
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Yonu, x.Tutar })
                .ToListAsync();

            var tr = new System.Globalization.CultureInfo("tr-TR");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine($"Müşteri;{musteriAdi};PB;{pb};Başlangıç;{start:yyyy-MM-dd};Bitiş;{end:yyyy-MM-dd};Filtre;{(showClosed ? "Kapanan" : "Açık/Kısmi")}");

            // showClosed ise "Kapanma" kolonu dahil
            sb.AppendLine(showClosed
                ? "Tarih;İşlem;Evrak No;Açıklama;Kapanma;Borç;Alacak;Bakiye"
                : "Tarih;İşlem;Evrak No;Açıklama;Borç;Alacak;Bakiye");

            decimal bakiye = acilis;
            sb.AppendLine($";;;Açılış Bakiyesi;{(showClosed ? ";" : "")};;{bakiye.ToString("N2", tr)}");

            foreach (var h in rows)
            {
                decimal borc = 0m, alacak = 0m;
                if (h.IslemTuru == "Sipariş" && h.Yonu == 1)
                {
                    alacak = h.Tutar;
                    borc = (h.EvrakNo != null && tahsilatSumByEvrak.TryGetValue(h.EvrakNo, out var t)) ? t : 0m;
                }
                else if (h.IslemTuru == "Manuel" && h.Yonu == 1)
                {
                    alacak = h.Tutar;
                    if (!string.IsNullOrEmpty(h.EvrakNo) && tahsilatSumByEvrak.TryGetValue(h.EvrakNo!, out var t2))
                        borc = t2;
                }
                else
                {
                    borc = h.Tutar; // evraksız tahsilatlar
                }

                var hasEvrak = !string.IsNullOrEmpty(h.EvrakNo);
                bool isDocRow = (h.IslemTuru == "Sipariş" && h.Yonu == 1) || (h.IslemTuru == "Manuel" && h.Yonu == 1);
                bool isClosed = hasEvrak && isDocRow
                                && alacakByEvrak.TryGetValue(h.EvrakNo!, out var alc)
                                && tahsilatSumByEvrak.TryGetValue(h.EvrakNo!, out var tah)
                                && tah >= alc;

                if (!showClosed && isClosed) continue;
                if (showClosed && !isClosed) continue;

                bakiye += (alacak - borc);

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
                cols.Add(borc.ToString("N2", tr));
                cols.Add(alacak.ToString("N2", tr));
                cols.Add(bakiye.ToString("N2", tr));

                sb.AppendLine(string.Join(";", cols));
            }

            var utf8 = System.Text.Encoding.UTF8;
            var bytes = utf8.GetPreamble().Concat(utf8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"Ekstre_{musteriAdi}_{pb}_{start:yyyyMMdd}-{end:yyyyMMdd}_{(showClosed ? "KAPANAN" : "ACIK")}.csv";
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
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb && ch.Tarih < start)
                .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
                .SumAsync();

            // --- EvrakNo bazlı sözlükler (alacak, tahsilat, kapanma) ---
            var alacakByEvrak = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && ((x.IslemTuru == "Sipariş" && x.Yonu == 1) || (x.IslemTuru == "Manuel" && x.Yonu == 1)))
                .GroupBy(x => x.EvrakNo!)
                .Select(g => new { EvrakNo = g.Key, Tutar = g.Sum(z => z.Tutar) })
                .ToDictionaryAsync(k => k.EvrakNo, v => v.Tutar);

            var tahsilatList = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && x.EvrakNo != null && ((x.IslemTuru == "Tahsilat" && x.Yonu == 0) || (x.IslemTuru == "Manuel" && x.Yonu == 0)))
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { EvrakNo = x.EvrakNo!, x.Tarih, x.Tutar })
                .ToListAsync();

            var tahsilatSumByEvrak = tahsilatList.GroupBy(g => g.EvrakNo).ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));
            var kapanmaTarihiByEvrak = tahsilatList.GroupBy(g => g.EvrakNo).ToDictionary(g => g.Key, g => g.Max(z => z.Tarih));

            var rows = await _context.CariHareketler.AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.MusteriID == musteriId && x.ParaBirimi == pb
                         && x.Tarih >= start && x.Tarih <= end
                         && (
                              (x.IslemTuru == "Sipariş" && x.Yonu == 1 && x.EvrakNo != null) ||
                              (x.IslemTuru == "Manuel" && x.Yonu == 1) ||
                              ((x.IslemTuru == "Tahsilat" || x.IslemTuru == "Manuel") && x.Yonu == 0 && x.EvrakNo == null)
                            ))
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Yonu, x.Tutar })
                .ToListAsync();

            // Listeyi, showClosed ise "Kapanma" alanıyla kur
            var list = new List<(string Tarih, string Islem, string EvrakNo, string Aciklama, string Kapanma, string Borc, string Alacak, string Bakiye)>();
            decimal bakiye = acilis;
            list.Add(("", "", "", "Açılış Bakiyesi", showClosed ? "" : "", "", "", bakiye.ToString("N2")));

            foreach (var h in rows)
            {
                decimal borc = 0m, alacak = 0m;
                if (h.IslemTuru == "Sipariş" && h.Yonu == 1)
                {
                    alacak = h.Tutar;
                    borc = (h.EvrakNo != null && tahsilatSumByEvrak.TryGetValue(h.EvrakNo, out var t)) ? t : 0m;
                }
                else if (h.IslemTuru == "Manuel" && h.Yonu == 1)
                {
                    alacak = h.Tutar;
                    if (!string.IsNullOrEmpty(h.EvrakNo) && tahsilatSumByEvrak.TryGetValue(h.EvrakNo!, out var t2))
                        borc = t2;
                }
                else
                {
                    borc = h.Tutar;
                }

                var hasEvrak = !string.IsNullOrEmpty(h.EvrakNo);
                bool isDocRow = (h.IslemTuru == "Sipariş" && h.Yonu == 1) || (h.IslemTuru == "Manuel" && h.Yonu == 1);
                bool isClosed = hasEvrak && isDocRow
                                && alacakByEvrak.TryGetValue(h.EvrakNo!, out var alc)
                                && tahsilatSumByEvrak.TryGetValue(h.EvrakNo!, out var tah)
                                && tah >= alc;

                if (!showClosed && isClosed) continue;
                if (showClosed && !isClosed) continue;

                bakiye += (alacak - borc);

                var kapanmaStr = (showClosed && isClosed && kapanmaTarihiByEvrak.TryGetValue(h.EvrakNo!, out var kt))
                    ? kt.ToString("dd.MM.yyyy") : "";

                list.Add((
                    h.Tarih.ToString("dd.MM.yyyy"),
                    h.IslemTuru ?? "",
                    string.IsNullOrWhiteSpace(h.EvrakNo) ? "" : h.EvrakNo!,
                    h.Aciklama ?? "",
                    kapanmaStr,
                    borc.ToString("N2"),
                    alacak.ToString("N2"),
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

                    page.Header().Text($"Cari Ekstre - {musteriAdi} / {pb} - {start:dd.MM.yyyy} - {end:dd.MM.yyyy}  ({(showClosed ? "Kapanan" : "Açık/Kısmi")})")
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
                                 .Text(t => { t.Span("LojistikDB • ").Light(); t.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}"); });
                });
            });

            var pdf = doc.GeneratePdf();
            var fileName = $"Ekstre_{musteriAdi}_{pb}_{start:yyyyMMdd}-{end:yyyyMMdd}_{(showClosed ? "KAPANAN" : "ACIK")}.pdf";
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
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb && ch.Tarih < start)
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
     (x.IslemTuru == "Sefer Alacak" && x.Yonu == 1)
   ))
                .OrderBy(x => x.Tarih).ThenBy(x => x.CariHareketID)
                .Select(x => new { x.CariHareketID, x.Tarih, x.IslemTuru, x.EvrakNo, x.Aciklama, x.Yonu, x.Tutar })
                .ToListAsync();

            // Satır satır bakiye
            Items.Clear();
            decimal bakiye = AcilisBakiye;

            foreach (var h in rows)
            {

                decimal borc = 0m, alacak = 0m;
                if ((h.IslemTuru == "Sipariş" && h.Yonu == 1) || (h.IslemTuru == "Manuel" && h.Yonu == 1) || (h.IslemTuru == "Sefer Alacak" && h.Yonu == 1))
                    alacak = h.Tutar;
                else
                    borc = h.Tutar;

                bakiye += (alacak - borc);

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
                    Kapandi = false,          // artık kullanılmıyor
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



            return Page();
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
