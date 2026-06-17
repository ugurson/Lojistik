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
        private readonly ICurrencyRateService _fx;
        public IndexModel(AppDbContext context, ICurrencyRateService fx)
        {
            _context = context;
            _fx = fx;
        }

        public bool EurKurOtomatik { get; set; } = false;

        public Dictionary<string, string> AltSubeAdMap { get; set; } = new();



        public record Row(
            int SeferID,
            string? SeferKodu,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            bool? Ozmal,
            byte Durum,
            string? AltSubeKodu,
            string? AltSubeAdi,
            int? SeferSuresiGun,
            int? KmMesafe
        );


        // ---- Filtreler / Params ----
        [BindProperty(SupportsGet = true)] public DateTime? Start { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public bool ozmal { get; set; } = true;
        [BindProperty(SupportsGet = true)] public bool showClosed { get; set; } = true;
        [BindProperty(SupportsGet = true)] public string? pbFilter { get; set; }

        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int pageSize { get; set; } = 20;
        [BindProperty(SupportsGet = true)] public int p { get; set; } = 1;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Clamp(pageSize, 10, 100));
        public IList<Row> Items { get; set; } = new List<Row>();

        public Dictionary<int, Dictionary<string, decimal>> GelirBySeferPB { get; set; } = new();
        public Dictionary<int, Dictionary<string, decimal>> MasrafBySeferPB { get; set; } = new();
        public Dictionary<int, decimal> YakitLitreBySeferID { get; set; } = new();

        public Dictionary<string, decimal> PageGelirPB { get; set; } = new();
        public Dictionary<string, decimal> PageMasrafPB { get; set; } = new();
        public Dictionary<string, decimal> PageNetPB { get; set; } = new();

        public Dictionary<string, decimal> RangeGelirPB { get; set; } = new();
        public Dictionary<string, decimal> RangeMasrafPB { get; set; } = new();
        public Dictionary<string, decimal> RangeNetPB { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string? altSube { get; set; }   // AltSubeKodu filtresi
        [BindProperty(SupportsGet = true)] public decimal EurKur { get; set; } = 0; // EUR→TL kur


        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var today = DateTime.Today;
            Start ??= new DateTime(today.Year, today.Month, 1);
            End ??= today;

            // EUR kuru otomatik çek (kullanıcı elle girmemişse)
            if (EurKur <= 0)
            {
                EurKur = await _fx.GetTryRateAsync("EUR", today);
                EurKurOtomatik = true;
            }

            var query = _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            // Tarih aralığı (Çıkış Tarihi)
            query = query.Where(s => s.CikisTarihi >= Start.Value.Date);
            query = query.Where(s => s.CikisTarihi < End.Value.Date.AddDays(1));

            // Açık/Kapalı
            if (showClosed)
                query = query.Where(s => s.Durum == 2);
            else
                query = query.Where(s => s.Durum != 2);

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
            if (!string.IsNullOrWhiteSpace(altSube))
            {
                var k = altSube.Trim();
                query = query.Where(s => s.SubeKodu != null && s.SubeKodu == k);
            }
            // Özmal (Arac.Ozmal INT 0/1)
            query = query.Where(s => s.Arac != null && s.Arac.Ozmal == (ozmal ? 1 : 0));

            // Sıralama
            query = query.OrderByDescending(s => s.CikisTarihi).ThenByDescending(s => s.SeferID);

            // PB filtresi listesi
            HashSet<string>? pbSet = null;
            if (!string.IsNullOrWhiteSpace(pbFilter))
            {
                pbSet = new HashSet<string>(
                    pbFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(x => x.Trim().ToUpperInvariant())
                );
            }

            // ===== ARALIK TOPLAMLARI (sayfalama yok) =====
            var seferIdsAllQuery = query.Select(s => s.SeferID);

            var rangeGelirAgg = await _context.SeferGelirleri.AsNoTracking()
                .Where(g => g.FirmaID == firmaId && seferIdsAllQuery.Contains(g.SeferID))
                .Select(g => new { PB = (g.ParaBirimi ?? "TL").Trim().ToUpper(), g.Tutar })
                .ToListAsync();

            if (pbSet != null) rangeGelirAgg = rangeGelirAgg.Where(x => pbSet.Contains(x.PB)).ToList();

            RangeGelirPB = rangeGelirAgg
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            var rangeMasrafAgg = await _context.SeferMasraflari.AsNoTracking()
                .Where(m => m.FirmaID == firmaId && seferIdsAllQuery.Contains(m.SeferID))
                .Select(m => new { PB = (m.ParaBirimi ?? "TL").Trim().ToUpper(), m.Tutar })
                .ToListAsync();

            if (pbSet != null) rangeMasrafAgg = rangeMasrafAgg.Where(x => pbSet.Contains(x.PB)).ToList();

            RangeMasrafPB = rangeMasrafAgg
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            var allRangePBs = new HashSet<string>(RangeGelirPB.Keys.Concat(RangeMasrafPB.Keys));
            foreach (var pb in allRangePBs)
            {
                var gSum = RangeGelirPB.TryGetValue(pb, out var gv) ? gv : 0m;
                var mSum = RangeMasrafPB.TryGetValue(pb, out var mv) ? mv : 0m;
                RangeNetPB[pb] = gSum - mSum;
            }

            // Sayfalama metrikleri
            TotalCount = await query.CountAsync();

            // Sayfa verileri (AltSubeAdi şimdilik null; map'ten sonra dolduracağız)
            Items = await query
                .Skip(Math.Max(0, (p - 1) * Math.Clamp(pageSize, 10, 100)))
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
                    s.Durum,
                    s.SubeKodu,
                    null,
                    s.CikisTarihi.HasValue && s.DonusTarihi.HasValue
                        ? (int?)((s.DonusTarihi.Value - s.CikisTarihi.Value).Days)
                        : null,
                    s.KmMesafe
                ))
                .ToListAsync();

            // ✅ Alt Şube Adları (Seferler.SubeKodu -> ProgramAltSubeler.AltSubeKodu -> AltSubeAdi)
            var kodlar = Items
                .Select(x => x.AltSubeKodu)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct()
                .ToList();


            if (kodlar.Count > 0)
            {
                AltSubeAdMap = await _context.ProgramAltSubeler
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && kodlar.Contains(a.AltSubeKodu))
                    .ToDictionaryAsync(a => a.AltSubeKodu, a => a.AltSubeAdi);

                Items = Items.Select(r =>
                {
                    var kod = r.AltSubeKodu?.Trim();
                    var ad = (kod != null && AltSubeAdMap.TryGetValue(kod, out var a)) ? a : null;
                    return r with { AltSubeAdi = ad };
                }).ToList();
            }

            var seferIds = Items.Select(i => i.SeferID).ToList();

            // GELİR
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

            // MASRAF
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

            // Sayfa üstü özetler
            PageGelirPB = gelirAgg
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            PageMasrafPB = masrafAgg
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            var allPBs = new HashSet<string>(PageGelirPB.Keys.Concat(PageMasrafPB.Keys));
            foreach (var pb in allPBs)
            {
                var gSum = PageGelirPB.TryGetValue(pb, out var gv) ? gv : 0m;
                var mSum = PageMasrafPB.TryGetValue(pb, out var mv) ? mv : 0m;
                PageNetPB[pb] = gSum - mSum;
            }

            // YAKIT LİTRESİ (sefer bazında)
            var yakitAgg = await _context.SeferMasraflari.AsNoTracking()
                .Where(m => m.FirmaID == firmaId && seferIds.Contains(m.SeferID) && m.MasrafTipi == "Yakıt" && m.YakitLitre != null)
                .Select(m => new { m.SeferID, Litre = m.YakitLitre!.Value })
                .ToListAsync();

            YakitLitreBySeferID = yakitAgg
                .GroupBy(x => x.SeferID)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Litre));
        }
    }
}
