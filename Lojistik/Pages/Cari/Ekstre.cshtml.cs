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
            public bool IptalEdilebilir =>
    string.Equals(IslemTuru, "Tahsilat", StringComparison.OrdinalIgnoreCase) && Alacak == 0 && Borc > 0
    || string.Equals(IslemTuru, "Manuel", StringComparison.OrdinalIgnoreCase);  // <-- eklendi

        }

        public List<Row> Items { get; set; } = new();

        // Totals (period)
        public decimal ToplamBorc { get; set; }
        public decimal ToplamAlacak { get; set; }
        public decimal DonemNet => ToplamAlacak - ToplamBorc;

        // Opening/Closing
        public decimal AcilisBakiye { get; set; }   // d1 öncesi net (PB)
        public decimal KapanisBakiye { get; set; }  // d2 sonu net (PB)
      
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

 // GET ile indiriyoruz
    public async Task<IActionResult> OnGetExportExcelAsync(int musteriId, string pb, DateTime? d1, DateTime? d2)
    {
        var firmaId = User.GetFirmaId();
        if (musteriId <= 0 || string.IsNullOrWhiteSpace(pb))
            return RedirectToPage("./Index");

        pb = pb.Trim().ToUpperInvariant();
        var start = d1?.Date ?? new DateTime(DateTime.Today.Year, 1, 1);
        var end = (d2?.Date ?? DateTime.Today);
        if (end < start) end = start;

        // Başlık bilgisi
        var musteriAdi = await _context.Musteriler.AsNoTracking()
            .Where(m => m.MusteriID == musteriId && m.FirmaID == firmaId)
            .Select(m => m.MusteriAdi)
            .FirstOrDefaultAsync() ?? $"#{musteriId}";

        // Açılış
        var acilis = await _context.CariHareketler.AsNoTracking()
            .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb && ch.Tarih < start)
            .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
            .SumAsync();

        // Dönem hareketleri
        var hs = await _context.CariHareketler.AsNoTracking()
            .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb
                      && ch.Tarih >= start && ch.Tarih <= end)
            .OrderBy(ch => ch.Tarih).ThenBy(ch => ch.CariHareketID)
            .Select(ch => new
            {
                ch.Tarih,
                ch.IslemTuru,
                ch.EvrakNo,
                ch.Aciklama,
                Borc = ch.Yonu == 0 ? ch.Tutar : 0m,
                Alacak = ch.Yonu == 1 ? ch.Tutar : 0m
            })
            .ToListAsync();

        var tr = new CultureInfo("tr-TR");
        var sb = new System.Text.StringBuilder();

        // Excel için ayraç ipucu (TR Excel virgül kullanır; biz ; kullanıyoruz)
        sb.AppendLine("sep=;");

        // Kapak satırı
        sb.AppendLine($"Müşteri;{musteriAdi};PB;{pb};Başlangıç;{start:yyyy-MM-dd};Bitiş;{end:yyyy-MM-dd}");

        // Başlıklar
        sb.AppendLine("Tarih;İşlem;Evrak No;Açıklama;Borç;Alacak;Bakiye");

        decimal bakiye = acilis;
        // Açılış satırı
        sb.AppendLine($";;;Açılış Bakiyesi;;;{bakiye.ToString("N2", tr)}");

        decimal tBorc = 0, tAlacak = 0;
        foreach (var h in hs)
        {
            bakiye += (h.Alacak - h.Borc);
            tBorc += h.Borc;
            tAlacak += h.Alacak;

            sb.AppendLine(string.Join(";", new[]
            {
            h.Tarih.ToString("dd.MM.yyyy", tr),
            h.IslemTuru ?? "",
            string.IsNullOrWhiteSpace(h.EvrakNo) ? "" : h.EvrakNo,
            (h.Aciklama ?? "").Replace(";", ","),
            h.Borc.ToString("N2", tr),
            h.Alacak.ToString("N2", tr),
            bakiye.ToString("N2", tr)
        }));
        }

        // Toplam satırı
        sb.AppendLine($";;;Toplam;{tBorc.ToString("N2", tr)};{tAlacak.ToString("N2", tr)};{bakiye.ToString("N2", tr)}");

        // UTF-8 BOM ile: Türkçe karakterler ve Excel uyumu
        var utf8 = System.Text.Encoding.UTF8;
        var bytes = utf8.GetPreamble().Concat(utf8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"Ekstre_{musteriAdi}_{pb}_{start:yyyyMMdd}-{end:yyyyMMdd}.csv";

        return File(bytes, "text/csv", fileName);
    }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnGetExportPdfAsync(int musteriId, string pb, DateTime? d1, DateTime? d2)
        {
            var firmaId = User.GetFirmaId();
            if (musteriId <= 0 || string.IsNullOrWhiteSpace(pb))
                return RedirectToPage("./Index");

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

            var hs = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && ch.MusteriID == musteriId && ch.ParaBirimi == pb
                          && ch.Tarih >= start && ch.Tarih <= end)
                .OrderBy(ch => ch.Tarih).ThenBy(ch => ch.CariHareketID)
                .Select(ch => new
                {
                    ch.Tarih,
                    ch.IslemTuru,
                    ch.EvrakNo,
                    ch.Aciklama,
                    Borc = ch.Yonu == 0 ? ch.Tutar : 0m,
                    Alacak = ch.Yonu == 1 ? ch.Tutar : 0m
                })
                .ToListAsync();

            decimal bakiye = acilis, tBorc = 0, tAlacak = 0;

            var rows = new List<(string Tarih, string Islem, string EvrakNo, string Aciklama, string Borc, string Alacak, string Bakiye)>();
            rows.Add(("", "", "", "Açılış Bakiyesi", "", "", acilis.ToString("N2")));

            foreach (var h in hs)
            {
                bakiye += (h.Alacak - h.Borc);
                tBorc += h.Borc;
                tAlacak += h.Alacak;

                rows.Add((
                    h.Tarih.ToString("dd.MM.yyyy"),
                    h.IslemTuru ?? "",
                    string.IsNullOrWhiteSpace(h.EvrakNo) ? "" : h.EvrakNo,
                    h.Aciklama ?? "",
                    h.Borc.ToString("N2"),
                    h.Alacak.ToString("N2"),
                    bakiye.ToString("N2")
                ));
            }

            var kapanis = bakiye;

            // ---- QuestPDF: tam nitelikli isimler ile ----
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Text($"Cari Ekstre - {musteriAdi} / {pb} - {start:dd.MM.yyyy} - {end:dd.MM.yyyy}")
                                 .SemiBold().FontSize(12);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);  // Tarih
                            c.RelativeColumn(16);  // İşlem
                            c.RelativeColumn(16);  // Evrak
                            c.RelativeColumn(36);  // Açıklama
                            c.ConstantColumn(22);  // Borç
                            c.ConstantColumn(22);  // Alacak
                            c.ConstantColumn(22);  // Bakiye
                        });

                        // Header
                        table.Header(h =>
                        {
                            h.Cell().Element(CellHeader).Text("Tarih");
                            h.Cell().Element(CellHeader).Text("İşlem");
                            h.Cell().Element(CellHeader).Text("Evrak");
                            h.Cell().Element(CellHeader).Text("Açıklama");
                            h.Cell().Element(CellHeader).AlignRight().Text($"Borç ({pb})");
                            h.Cell().Element(CellHeader).AlignRight().Text($"Alacak ({pb})");
                            h.Cell().Element(CellHeader).AlignRight().Text($"Bakiye ({pb})");

                            static QuestPDF.Infrastructure.IContainer CellHeader(QuestPDF.Infrastructure.IContainer c)
                                => c.DefaultTextStyle(x => x.SemiBold())
                                    .PaddingVertical(4).PaddingHorizontal(3)
                                    .BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Medium);
                        });

                        // Rows
                        foreach (var r in rows)
                        {
                            table.Cell().Element(Cell).Text(r.Tarih);
                            table.Cell().Element(Cell).Text(r.Islem);
                            table.Cell().Element(Cell).Text(r.EvrakNo);
                            table.Cell().Element(Cell).Text(r.Aciklama);
                            table.Cell().Element(Cell).AlignRight().Text(r.Borc);
                            table.Cell().Element(Cell).AlignRight().Text(r.Alacak);
                            table.Cell().Element(Cell).AlignRight().Text(r.Bakiye);

                            static QuestPDF.Infrastructure.IContainer Cell(QuestPDF.Infrastructure.IContainer c)
                                => c.PaddingVertical(2).PaddingHorizontal(3);
                        }

                        // Totals
                        table.Cell().ColumnSpan(4).Element(FooterCell).AlignRight().Text("Toplam");
                        table.Cell().Element(FooterCell).AlignRight().Text(tBorc.ToString("N2"));
                        table.Cell().Element(FooterCell).AlignRight().Text(tAlacak.ToString("N2"));
                        table.Cell().Element(FooterCell).AlignRight().Text(kapanis.ToString("N2"));

                        static QuestPDF.Infrastructure.IContainer FooterCell(QuestPDF.Infrastructure.IContainer c)
                            => c.PaddingVertical(4).PaddingHorizontal(3)
                                 .BorderTop(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Medium)
                                 .DefaultTextStyle(x => x.SemiBold());
                    });

                    page.Footer()
     .DefaultTextStyle(x => x.FontSize(8))   // font boyutunu burada ver
     .AlignRight()
     .Text(txt =>
     {
         txt.Span("LojistikDB • ").Light();
         txt.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
     });

                });
            });

            var pdf = doc.GeneratePdf();
            var fileName = $"Ekstre_{musteriAdi}_{pb}_{start:yyyyMMdd}-{end:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", fileName);
        }


        public async Task<IActionResult> OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            if (musteriId <= 0 || string.IsNullOrWhiteSpace(pb))
                return RedirectToPage("./Index");

            pb = pb.Trim().ToUpperInvariant();

            // Tarih aralığı varsayılanları
            var start = d1?.Date ?? new DateTime(DateTime.Today.Year, 1, 1);
            var end = (d2?.Date ?? DateTime.Today);
            if (end < start) end = start;

            // Müşteri adı
            MusteriAdi = await _context.Musteriler.AsNoTracking()
                .Where(m => m.MusteriID == musteriId && m.FirmaID == firmaId)
                .Select(m => m.MusteriAdi)
                .FirstOrDefaultAsync() ?? $"#{musteriId}";

            // Açılış bakiye: start tarihinden ÖNCEKİ tüm hareketlerin neti
            AcilisBakiye = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId
                          && ch.MusteriID == musteriId
                          && ch.ParaBirimi == pb
                          && ch.Tarih < start)
                .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
                .SumAsync();

            // Dönem hareketleri (start..end dahil)
            var hareketler = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId
                          && ch.MusteriID == musteriId
                          && ch.ParaBirimi == pb
                          && ch.Tarih >= start
                          && ch.Tarih <= end)
                .OrderBy(ch => ch.Tarih)
                .ThenBy(ch => ch.CariHareketID)
                .Select(ch => new
                {
                    ch.CariHareketID,
                    ch.Tarih,
                    ch.IslemTuru,
                    ch.EvrakNo,
                    ch.Aciklama,
                    Borc = ch.Yonu == 0 ? ch.Tutar : 0m,
                    Alacak = ch.Yonu == 1 ? ch.Tutar : 0m
                })
                .ToListAsync();

            // Running bakiye
            decimal bakiye = AcilisBakiye;
            foreach (var h in hareketler)
            {
                bakiye += (h.Alacak - h.Borc);
                Items.Add(new Row
                {
                    CariHareketID = h.CariHareketID,
                    Tarih = h.Tarih,
                    IslemTuru = h.IslemTuru,
                    EvrakNo = h.EvrakNo,
                    Aciklama = h.Aciklama,
                    Borc = h.Borc,
                    Alacak = h.Alacak,
                    Bakiye = bakiye
                });
            }

            // Totals
            ToplamBorc = hareketler.Sum(x => x.Borc);
            ToplamAlacak = hareketler.Sum(x => x.Alacak);

            // Closing
            KapanisBakiye = bakiye;

            // Varsayılan tarihleri geri yaz (UI'da görünsün)
            d1 = start;
            d2 = end;

            return Page();
        }
    }
}
