// Pages/Raporlar/Sefer/Index.cshtml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar.Sefer
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SeferID,
            string? SeferKodu,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            bool? Ozmal,
            byte Durum
        );

        // ---- Filtreler / Params ----
        [BindProperty(SupportsGet = true)] public DateTime? Start { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public bool ozmal { get; set; } = true; // öncelik özmal
        [BindProperty(SupportsGet = true)] public bool showClosed { get; set; } = false; // Kapalı seferleri göster

        // PB filtresi (opsiyonel): boş ise hepsi; "TL,EUR" gibi virgüllü verilebilir
        [BindProperty(SupportsGet = true)] public string? pbFilter { get; set; }

        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int pageSize { get; set; } = 20;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Clamp(pageSize, 10, 100));

        public IList<Row> Items { get; set; } = new List<Row>();

        // Satır başına PB bazında gelir/gider toplamları
        public Dictionary<int, Dictionary<string, decimal>> GelirBySeferPB { get; set; } = new();
        public Dictionary<int, Dictionary<string, decimal>> MasrafBySeferPB { get; set; } = new();

        // Sayfadaki (görünen satırlar) için PB bazında özetler
        public Dictionary<string, decimal> PageGelirPB { get; set; } = new();
        public Dictionary<string, decimal> PageMasrafPB { get; set; } = new();
        public Dictionary<string, decimal> PageNetPB { get; set; } = new();

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var query = _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            // Tarih aralığı (Çıkış Tarihi)
            if (Start.HasValue) query = query.Where(s => s.CikisTarihi >= Start.Value.Date);
            if (End.HasValue) query = query.Where(s => s.CikisTarihi < End.Value.Date.AddDays(1));

            // Açık/Kapalı
            if (showClosed)
                query = query.Where(s => s.Durum == 2); // kapalı
            else
                query = query.Where(s => s.Durum != 2); // açık/kısmi

            // Arama
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(s =>
                    (s.SeferKodu != null && s.SeferKodu.Contains(term)) ||
                    (s.SurucuAdi != null && s.SurucuAdi.Contains(term)) ||
                    (s.Arac != null && s.Arac.Plaka.Contains(term)) ||
                    (s.Dorse != null && s.Dorse.Plaka.Contains(term))
                );
            }

            // Özmal (Arac.Ozmal INT 0/1)
            query = query.Where(s => s.Arac != null && s.Arac.Ozmal == (ozmal ? 1 : 0));

            // Sıralama
            query = query.OrderByDescending(s => s.CikisTarihi).ThenByDescending(s => s.SeferID);

            // Sayfalama metrikleri
            TotalCount = await query.CountAsync();

            // Sayfa verileri
            Items = await query
                .Skip(Math.Max(0, (page - 1) * Math.Clamp(pageSize, 10, 100)))
                .Take(Math.Clamp(pageSize, 10, 100))
                .Select(s => new Row(
                    s.SeferID,
                    s.SeferKodu,
                    s.CikisTarihi,
                    s.DonusTarihi,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.Arac != null ? (bool?)(s.Arac.Ozmal == 1) : null,
                    s.Durum
                ))
                .ToListAsync();

            var seferIds = Items.Select(i => i.SeferID).ToList();

            // İsteğe bağlı PB filtresi listesi
            HashSet<string>? pbSet = null;
            if (!string.IsNullOrWhiteSpace(pbFilter))
            {
                pbSet = new HashSet<string>(
                    pbFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(x => x.ToUpperInvariant())
                );
            }

            // Seçili sayfadaki seferler için PB bazında GELİR toplamları
            var gelirAgg = await _context.SeferGelirleri.AsNoTracking()
                .Where(g => g.FirmaID == firmaId && seferIds.Contains(g.SeferID))
                .Select(g => new { g.SeferID, PB = (g.ParaBirimi ?? "TL").Trim().ToUpper(), g.Tutar })
                .ToListAsync();

            if (pbSet != null) gelirAgg = gelirAgg.Where(x => pbSet.Contains(x.PB)).ToList();

            GelirBySeferPB = gelirAgg
                .GroupBy(x => x.SeferID)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.PB).ToDictionary(gg => gg.Key, gg => gg.Sum(z => z.Tutar))
                );

            // Seçili sayfadaki seferler için PB bazında MASRAF toplamları
            var masrafAgg = await _context.SeferMasraflari.AsNoTracking()
                .Where(m => m.FirmaID == firmaId && seferIds.Contains(m.SeferID))
                .Select(m => new { m.SeferID, PB = (m.ParaBirimi ?? "TL").Trim().ToUpper(), m.Tutar })
                .ToListAsync();

            if (pbSet != null) masrafAgg = masrafAgg.Where(x => pbSet.Contains(x.PB)).ToList();

            MasrafBySeferPB = masrafAgg
                .GroupBy(x => x.SeferID)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.PB).ToDictionary(gg => gg.Key, gg => gg.Sum(z => z.Tutar))
                );

            // Sayfa üstü özetler (görünen satırlara göre)
            PageGelirPB = gelirAgg
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            PageMasrafPB = masrafAgg
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            // Net = Gelir - Masraf (PB bazında)
            var allPBs = new HashSet<string>(PageGelirPB.Keys.Concat(PageMasrafPB.Keys));
            foreach (var pb in allPBs)
            {
                var gSum = PageGelirPB.TryGetValue(pb, out var gv) ? gv : 0m;
                var mSum = PageMasrafPB.TryGetValue(pb, out var mv) ? mv : 0m;
                PageNetPB[pb] = gSum - mSum;
            }
        }
    }
}
