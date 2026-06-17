using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Lojistik.Data;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Cari
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public class Row
        {
            public int MusteriID { get; set; }
            public string? MusteriAdi { get; set; }
            public string ParaBirimi { get; set; } = "TL";
            public decimal BorcPB { get; set; }     // Yonu=1 → Borç (müşteri bize borçlu)
            public decimal AlacakPB { get; set; }   // Yonu=0 → Alacak (müşterinin ödemesi)
            public decimal NetPB => BorcPB - AlacakPB; // (+) müşteri borçlu
            public bool DevirSifir { get; set; } = false;
            public DateTime? SonIslemTarihi { get; set; }
        }

        public class TotalRow
        {
            public string ParaBirimi { get; set; } = "TL";
            public decimal BorcPB { get; set; }
            public decimal AlacakPB { get; set; }
            public decimal NetPB => BorcPB - AlacakPB;
        }

        public IList<TotalRow> Totals { get; set; } = new List<TotalRow>();
        public IList<Row> Items { get; set; } = new List<Row>();
        public string? q { get; set; }
        public bool showZero { get; set; } = false;

        public async Task OnGetAsync(string? q, bool showZero = false)
        {
            this.q = q;
            this.showZero = showZero;
            var firmaId = User.GetFirmaId();

            // 1) Aktif hareketlerden bakiyeler
            var grouped = await _context.CariHareketler
                .AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && !ch.IsArsiv)
                .GroupBy(ch => new { ch.MusteriID, ch.ParaBirimi })
                .Select(g => new
                {
                    g.Key.MusteriID,
                    g.Key.ParaBirimi,
                    BorcPB   = g.Sum(x => x.Yonu == 1 ? x.Tutar : 0m),
                    AlacakPB = g.Sum(x => x.Yonu == 0 ? x.Tutar : 0m)
                })
                .ToListAsync();

            // 2) Arşivde kaydı olan ama aktif kaydı olmayan (devir alınmış sıfır bakiyeli) müşteriler
            var arsivPairs = await _context.CariHareketler
                .AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && ch.IsArsiv)
                .GroupBy(ch => new { ch.MusteriID, ch.ParaBirimi })
                .Select(g => new { g.Key.MusteriID, g.Key.ParaBirimi })
                .ToListAsync();

            var activeKeys = grouped
                .Select(x => (x.MusteriID, x.ParaBirimi))
                .ToHashSet();

            var sifirlar = arsivPairs
                .Where(x => !activeKeys.Contains((x.MusteriID, x.ParaBirimi)))
                .Select(x => new
                {
                    x.MusteriID,
                    x.ParaBirimi,
                    BorcPB   = 0m,
                    AlacakPB = 0m
                })
                .ToList();

            // 3) Müşteri adlarını çek
            var tumMusteriIds = grouped.Select(x => x.MusteriID)
                .Concat(sifirlar.Select(x => x.MusteriID))
                .Distinct()
                .ToList();

            var adlar = await _context.Musteriler
                .AsNoTracking()
                .Where(m => tumMusteriIds.Contains(m.MusteriID))
                .Select(m => new { m.MusteriID, m.MusteriAdi })
                .ToListAsync();

            var dictAd = adlar.ToDictionary(x => x.MusteriID, x => x.MusteriAdi);

            // 4) Son işlem tarihleri (aktif hareketler)
            var sonIslemler = await _context.CariHareketler
                .AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && !ch.IsArsiv)
                .GroupBy(ch => new { ch.MusteriID, ch.ParaBirimi })
                .Select(g => new { g.Key.MusteriID, g.Key.ParaBirimi, SonTarih = g.Max(x => x.Tarih) })
                .ToListAsync();
            var sonIslemDict = sonIslemler.ToDictionary(x => (x.MusteriID, x.ParaBirimi), x => (DateTime?)x.SonTarih);

            // 5) Satırları birleştir
            var rows = grouped.Select(x => new Row
            {
                MusteriID       = x.MusteriID,
                MusteriAdi      = dictAd.TryGetValue(x.MusteriID, out var ad) ? ad : "(kaynak yok)",
                ParaBirimi      = x.ParaBirimi,
                BorcPB          = x.BorcPB,
                AlacakPB        = x.AlacakPB,
                DevirSifir      = false,
                SonIslemTarihi  = sonIslemDict.TryGetValue((x.MusteriID, x.ParaBirimi), out var st) ? st : null
            })
            .Concat(sifirlar.Select(x => new Row
            {
                MusteriID  = x.MusteriID,
                MusteriAdi = dictAd.TryGetValue(x.MusteriID, out var ad) ? ad : "(kaynak yok)",
                ParaBirimi = x.ParaBirimi,
                BorcPB     = 0m,
                AlacakPB   = 0m,
                DevirSifir = true
            }));

            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLowerInvariant();
                rows = rows.Where(r => (r.MusteriAdi ?? "").ToLower().Contains(ql));
            }

            if (!showZero)
                rows = rows.Where(r => !r.DevirSifir);

            Items = rows
                .OrderBy(r => r.ParaBirimi)
                .ThenByDescending(r => Math.Abs(r.NetPB))
                .ThenBy(r => r.MusteriAdi)
                .ToList();

            // PB bazında alt toplamlar (sadece aktif bakiyelerden)
            Totals = Items
                .GroupBy(i => i.ParaBirimi)
                .Select(g => new TotalRow
                {
                    ParaBirimi = g.Key,
                    BorcPB     = g.Sum(x => x.BorcPB),
                    AlacakPB   = g.Sum(x => x.AlacakPB)
                })
                .OrderBy(t => t.ParaBirimi)
                .ToList();
        }
    }
}
