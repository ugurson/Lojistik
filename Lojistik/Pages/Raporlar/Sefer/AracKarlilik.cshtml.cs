using System;
using System.Collections.Generic;
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
    public class AracKarlilikModel : PageModel
    {
        private readonly AppDbContext _context;
        public AracKarlilikModel(AppDbContext context) => _context = context;

        public record AracRow(
            int AracID,
            string Plaka,
            string? Marka,
            string? Model,
            int SeferSayisi,
            double OrtSeferSuresiGun,
            int ToplamKm,
            decimal ToplamYakitLitre,
            Dictionary<string, decimal> ToplamGelirPB,
            Dictionary<string, decimal> ToplamMasrafPB
        );

        [BindProperty(SupportsGet = true)] public DateTime? Start { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public bool showClosed { get; set; } = true;

        public IList<AracRow> Items { get; set; } = new List<AracRow>();
        public Dictionary<string, decimal> ToplamGelirPB { get; set; } = new();
        public Dictionary<string, decimal> ToplamMasrafPB { get; set; } = new();

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();
            var today = DateTime.Today;
            Start ??= new DateTime(today.Year, today.Month, 1);
            End ??= today;

            var seferQuery = _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId
                    && s.CikisTarihi >= Start.Value.Date
                    && s.CikisTarihi < End.Value.Date.AddDays(1));

            if (showClosed)
                seferQuery = seferQuery.Where(s => s.Durum == 2);

            var seferler = await seferQuery
                .Select(s => new
                {
                    s.SeferID,
                    s.AracID,
                    Plaka     = s.Arac != null ? s.Arac.Plaka : "?",
                    Marka     = s.Arac != null ? s.Arac.Marka : null,
                    AracModel = s.Arac != null ? s.Arac.Model : null,
                    s.CikisTarihi,
                    s.DonusTarihi,
                    s.KmMesafe
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                seferler = seferler.Where(s => s.Plaka.ToLower().Contains(term)).ToList();
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
                .GroupBy(s => s.AracID)
                .Select(g =>
                {
                    var first = g.First();
                    var grpIds = g.Select(x => x.SeferID).ToHashSet();
                    var grpGelir = gelirler.Where(x => grpIds.Contains(x.SeferID)).ToList();
                    var grpMasraf = masraflar.Where(x => grpIds.Contains(x.SeferID)).ToList();
                    var sureler = g
                        .Where(s => s.CikisTarihi.HasValue && s.DonusTarihi.HasValue)
                        .Select(s => (s.DonusTarihi!.Value - s.CikisTarihi!.Value).TotalDays)
                        .ToList();
                    return new AracRow(
                        g.Key,
                        first.Plaka,
                        first.Marka,
                        first.AracModel,
                        g.Count(),
                        sureler.Count > 0 ? Math.Round(sureler.Average(), 1) : 0,
                        g.Sum(s => s.KmMesafe ?? 0),
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
    }
}
