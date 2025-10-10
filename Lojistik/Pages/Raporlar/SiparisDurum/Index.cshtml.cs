using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;                    // LINQ
using ClosedXML.Excel;
using Lojistik.Data;
using Lojistik.Extensions;            // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar.SiparisDurum
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        // ---- Filtreler ----
        [BindProperty(SupportsGet = true), DataType(DataType.Date)]
        public DateTime? Start { get; set; }

        [BindProperty(SupportsGet = true), DataType(DataType.Date)]
        public DateTime? End { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<byte>? Durumlar { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PB { get; set; }

        // ---- Sonuçlar ----
        public List<SummaryRow> Ozet { get; set; } = new();
        public List<DetailRow> Detay { get; set; } = new();

        public int ToplamAdet => Ozet.Sum(x => x.Adet);

        public class SummaryRow
        {
            public byte Durum { get; set; }
            public string DurumAd =>
                Durum switch { 0 => "Açık", 1 => "Yüklendi", 2 => "Sevk", 7 => "Teslim Edildi", _ => $"Durum {Durum}" };

            public string? ParaBirimi { get; set; }
            public int Adet { get; set; }
            public decimal? ToplamTutar { get; set; }
        }

        public class DetailRow
        {
            public int SiparisID { get; set; }
            public DateTime Tarih { get; set; }
            public byte Durum { get; set; }
            public string DurumAd =>
                Durum switch { 0 => "Açık", 1 => "Yüklendi", 2 => "Sevk", 7 => "Teslim Edildi", _ => $"Durum {Durum}" };
            public decimal? Kilo { get; set; }
            public decimal? Tutar { get; set; }
            public string? ParaBirimi { get; set; }
            public string? Gonderen { get; set; }
            public string? Alici { get; set; }
            public string? AliciUlke { get; set; }
        }

        private static string DurumToAd(byte d) =>
            d switch { 0 => "Açık", 1 => "Planlandı", 2 => "Yüklendi", 3 => "Teslim Edildi", 9 => "İptal", _ => $"Durum {d}" };

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            // [start, end) mantığı
            var start = (Start ?? DateTime.Today.AddDays(-30)).Date;
            var end = ((End ?? DateTime.Today).Date).AddDays(1);

            // Temel sorgu
            var query = _context.Siparisler
                .AsNoTracking()
                .Include(s => s.GonderenMusteri)
                .Include(s => s.AliciMusteri)
                    .ThenInclude(m => m.Ulke) // Alici ülke navigation
                .Where(s => s.FirmaID == firmaId)
                .Where(s => s.SiparisTarihi >= start && s.SiparisTarihi < end);

            // Arama
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (int.TryParse(term, out var id))
                {
                    query = query.Where(s => s.SiparisID == id);
                }
                else
                {
                    query = query.Where(s =>
                        (s.FaturaNo ?? "").Contains(term) ||
                        (s.GonderenMusteri != null && (s.GonderenMusteri.MusteriAdi ?? "").Contains(term)) ||
                        (s.AliciMusteri != null && (s.AliciMusteri.MusteriAdi ?? "").Contains(term))
                    );
                }
            }

            if (Durumlar is { Count: > 0 })
                query = query.Where(s => Durumlar.Contains(s.Durum));

            if (!string.IsNullOrEmpty(PB))
                query = query.Where(s => s.ParaBirimi == PB);

            // ---- Özet (Durum + PB) ----  (ToplamKilo kaldırıldı)
            Ozet = await query
                .GroupBy(s => new { s.Durum, s.ParaBirimi })
                .Select(g => new SummaryRow
                {
                    Durum = g.Key.Durum,
                    ParaBirimi = g.Key.ParaBirimi,
                    Adet = g.Count(),
                    ToplamTutar = g.Sum(x => (decimal?)x.Tutar ?? 0)
                })
                .OrderBy(r => r.Durum).ThenBy(r => r.ParaBirimi)
                .ToListAsync();

            // ---- Detay (ilk 1000) ----  (AliciUlke eklendi)
            Detay = await query
                .OrderByDescending(s => s.SiparisTarihi)
                .Take(1000)
                .Select(s => new DetailRow
                {
                    SiparisID = s.SiparisID,
                    Tarih = s.SiparisTarihi,
                    Durum = s.Durum,
                    Kilo = s.Kilo,
                    Tutar = s.Tutar,
                    ParaBirimi = s.ParaBirimi,
                    Gonderen = s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    Alici = s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    AliciUlke = s.AliciMusteri != null ? (s.AliciMusteri.Ulke != null ? s.AliciMusteri.Ulke.UlkeAdi : null) : null
                })
                .ToListAsync();
        }

        // ---- Excel dışa aktarma ----
        public async Task<IActionResult> OnGetExportAsync()
        {
            var firmaId = User.GetFirmaId();

            var start = (Start ?? DateTime.Today.AddDays(-30)).Date;
            var end = ((End ?? DateTime.Today).Date).AddDays(1);

            var query = _context.Siparisler
                .AsNoTracking()
                .Include(s => s.GonderenMusteri)
                .Include(s => s.AliciMusteri)
                    .ThenInclude(m => m.Ulke)
                .Where(s => s.FirmaID == firmaId)
                .Where(s => s.SiparisTarihi >= start && s.SiparisTarihi < end);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (int.TryParse(term, out var id))
                {
                    query = query.Where(s => s.SiparisID == id);
                }
                else
                {
                    query = query.Where(s =>
                        (s.FaturaNo ?? "").Contains(term) ||
                        (s.GonderenMusteri != null && (s.GonderenMusteri.MusteriAdi ?? "").Contains(term)) ||
                        (s.AliciMusteri != null && (s.AliciMusteri.MusteriAdi ?? "").Contains(term))
                    );
                }
            }
            if (Durumlar is { Count: > 0 })
                query = query.Where(s => Durumlar.Contains(s.Durum));
            if (!string.IsNullOrEmpty(PB))
                query = query.Where(s => s.ParaBirimi == PB);

            // EF dışına aldıktan sonra format/switch uygula
            var raw = await query
                .OrderByDescending(s => s.SiparisTarihi)
                .Select(s => new
                {
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.Durum,
                    s.Kilo,
                    s.Tutar,
                    s.ParaBirimi,
                    Gonderen = s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    Alici = s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    AliciUlke = s.AliciMusteri != null ? (s.AliciMusteri.Ulke != null ? s.AliciMusteri.Ulke.UlkeAdi : null) : null,
                    s.FaturaNo
                })
                .ToListAsync();

            var data = raw.Select(s => new
            {
                s.SiparisID,
                Tarih = s.SiparisTarihi.ToString("dd.MM.yyyy"),
                Durum = DurumToAd(s.Durum),
                s.Kilo,
                s.Gonderen,
                s.Alici,
                s.AliciUlke,
                s.Tutar,
                s.ParaBirimi,
                s.FaturaNo
            }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("SiparisDurum");
            ws.Cell(1, 1).InsertTable(data);
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            var fileName = $"SiparisDurum_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
