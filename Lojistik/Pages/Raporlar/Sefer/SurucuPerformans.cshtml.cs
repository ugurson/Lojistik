using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar.Sefer
{
    [Authorize]
    public class SurucuPerformansModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ICurrencyRateService _fx;

        public SurucuPerformansModel(AppDbContext context, ICurrencyRateService fx)
        {
            _context = context;
            _fx = fx;
        }

        public record SurucuRow(
            string SurucuAdi,
            int SeferSayisi,
            double OrtSeferSuresiGun,
            int? ToplamKm,
            decimal ToplamYakitLitre,
            Dictionary<string, decimal> ToplamGelirPB,
            Dictionary<string, decimal> ToplamMasrafPB
        )
        {
            // Yardımcı: PB -> Tutar farkı (Gelir - Masraf)
            public decimal NetPB(string pb)
            {
                var g = ToplamGelirPB.TryGetValue(pb, out var gv) ? gv : 0m;
                var m = ToplamMasrafPB.TryGetValue(pb, out var mv) ? mv : 0m;
                return g - m;
            }
        }

        [BindProperty(SupportsGet = true)] public DateTime? Start { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public bool showClosed { get; set; } = true;

        // Kur değerleri string olarak alınır; tr-TR ↔ InvariantCulture çakışması önlenir
        [BindProperty(SupportsGet = true)] public string? EurKurStr { get; set; }
        [BindProperty(SupportsGet = true)] public string? UsdKurStr { get; set; }
        public decimal EurKur { get; private set; } = 0;
        public decimal UsdKur { get; private set; } = 0;

        public bool EurKurOtomatik { get; set; }
        public bool UsdKurOtomatik { get; set; }

        /// Hem "34.5678" (InvariantCulture) hem "34,5678" (tr-TR) formatını kabul eder
        private static decimal ParseKur(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            // Kur değerinde binlik ayraç beklenmez; virgülü noktaya çevir, InvariantCulture ile parse et
            var normalized = s.Trim().Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out var v) && v > 0 ? v : 0;
        }

        public IList<SurucuRow> Items { get; set; } = new List<SurucuRow>();
        public Dictionary<string, decimal> ToplamGelirPB { get; set; } = new();
        public Dictionary<string, decimal> ToplamMasrafPB { get; set; } = new();

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();
            var today = DateTime.Today;
            Start ??= new DateTime(today.Year, today.Month, 1);
            End ??= today;

            EurKur = ParseKur(EurKurStr);
            UsdKur = ParseKur(UsdKurStr);

            if (EurKur <= 0) { EurKur = await _fx.GetTryRateAsync("EUR", today); EurKurOtomatik = true; }
            if (UsdKur <= 0) { UsdKur = await _fx.GetTryRateAsync("USD", today); UsdKurOtomatik = true; }

            var seferQuery = _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId
                    && s.CikisTarihi >= Start.Value.Date
                    && s.CikisTarihi < End.Value.Date.AddDays(1)
                    && s.SurucuAdi != null);

            if (showClosed)
                seferQuery = seferQuery.Where(s => s.Durum == 2);

            var seferler = await seferQuery
                .Select(s => new
                {
                    s.SeferID,
                    s.SurucuAdi,
                    s.CikisTarihi,
                    s.DonusTarihi,
                    s.KmMesafe
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                seferler = seferler.Where(s => s.SurucuAdi!.ToLower().Contains(term)).ToList();
            }

            var seferIds = seferler.Select(s => s.SeferID).ToList();

            var gelirler = await _context.SeferGelirleri.AsNoTracking()
                .Where(g => g.FirmaID == firmaId && seferIds.Contains(g.SeferID))
                .Select(g => new { g.SeferID, PB = (g.ParaBirimi ?? "TL").Trim().ToUpper(), g.Tutar })
                .ToListAsync();

            var masraflar = await _context.SeferMasraflari.AsNoTracking()
                .Where(m => m.FirmaID == firmaId && seferIds.Contains(m.SeferID))
                .Select(m => new { m.SeferID, PB = (m.ParaBirimi ?? "TL").Trim().ToUpper(), m.Tutar, m.MasrafTipi, Litre = m.YakitLitre })
                .ToListAsync();

            Items = seferler
                .GroupBy(s => s.SurucuAdi!.Trim())
                .Select(g =>
                {
                    var grpIds = g.Select(x => x.SeferID).ToHashSet();
                    var grpGelir = gelirler.Where(x => grpIds.Contains(x.SeferID)).ToList();
                    var grpMasraf = masraflar.Where(x => grpIds.Contains(x.SeferID)).ToList();
                    var sureler = g
                        .Where(s => s.CikisTarihi.HasValue && s.DonusTarihi.HasValue)
                        .Select(s => (s.DonusTarihi!.Value - s.CikisTarihi!.Value).TotalDays)
                        .ToList();
                    return new SurucuRow(
                        g.Key,
                        g.Count(),
                        sureler.Count > 0 ? Math.Round(sureler.Average(), 1) : 0,
                        g.Sum(s => s.KmMesafe),
                        grpMasraf.Where(x => x.MasrafTipi == "Yakıt").Sum(x => x.Litre ?? 0m),
                        grpGelir.GroupBy(x => x.PB).ToDictionary(gg => gg.Key, gg => gg.Sum(z => z.Tutar)),
                        grpMasraf.GroupBy(x => x.PB).ToDictionary(gg => gg.Key, gg => gg.Sum(z => z.Tutar))
                    );
                })
                .OrderByDescending(r => r.SeferSayisi)
                .ToList();

            ToplamGelirPB = gelirler
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));

            ToplamMasrafPB = masraflar
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));
        }

        // Tüm PB'leri TL'ye çevirip toplar
        public decimal HesaplaNetTL(Dictionary<string, decimal> gelirPB, Dictionary<string, decimal> masrafPB)
        {
            decimal net = 0m;
            var tumPBler = new HashSet<string>(gelirPB.Keys.Concat(masrafPB.Keys));
            foreach (var pb in tumPBler)
            {
                var g = gelirPB.TryGetValue(pb, out var gv) ? gv : 0m;
                var m = masrafPB.TryGetValue(pb, out var mv) ? mv : 0m;
                var fark = g - m;
                net += pb switch
                {
                    "EUR" => fark * EurKur,
                    "USD" => fark * UsdKur,
                    _     => fark
                };
            }
            return net;
        }
    }
}
