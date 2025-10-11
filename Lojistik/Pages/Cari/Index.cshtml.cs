using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
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
            public decimal BorcPB { get; set; }     // Yonu=0 → Borç
            public decimal AlacakPB { get; set; }   // Yonu=1 → Alacak
            public decimal NetPB => AlacakPB - BorcPB; // (+) alacaklıyız
        }
        public class TotalRow
        {
            public string ParaBirimi { get; set; } = "TL";
            public decimal BorcPB { get; set; }
            public decimal AlacakPB { get; set; }
            public decimal NetPB => AlacakPB - BorcPB;
        }

        public IList<TotalRow> Totals { get; set; } = new List<TotalRow>();

        public IList<Row> Items { get; set; } = new List<Row>();
        public string? q { get; set; }

        public async Task OnGetAsync(string? q)
        {
            this.q = q;
            var firmaId = User.GetFirmaId();

            // Firma filtresi + MusteriID & ParaBirimi kırılımı
            var grouped = await _context.CariHareketler
                .AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId)
                .GroupBy(ch => new { ch.MusteriID, ch.ParaBirimi })
                .Select(g => new
                {
                    g.Key.MusteriID,
                    g.Key.ParaBirimi,
                    BorcPB = g.Sum(x => x.Yonu == 0 ? x.Tutar : 0m),
                    AlacakPB = g.Sum(x => x.Yonu == 1 ? x.Tutar : 0m)
                })
                .ToListAsync();

            var musteriIds = grouped.Select(x => x.MusteriID).Distinct().ToList();

            var adlar = await _context.Musteriler
                .AsNoTracking()
                .Where(m => musteriIds.Contains(m.MusteriID))
                .Select(m => new { m.MusteriID, m.MusteriAdi })
                .ToListAsync();

            var dictAd = adlar.ToDictionary(x => x.MusteriID, x => x.MusteriAdi);

            var rows = grouped.Select(x => new Row
            {
                MusteriID = x.MusteriID,
                MusteriAdi = dictAd.TryGetValue(x.MusteriID, out var ad) ? ad : "(kaynak yok)",
                ParaBirimi = x.ParaBirimi,
                BorcPB = x.BorcPB,
                AlacakPB = x.AlacakPB
            });

            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.Trim().ToLowerInvariant();
                rows = rows.Where(r => (r.MusteriAdi ?? "").ToLower().Contains(ql));
            }

            // Net alacak çoğundan aza
            Items = rows
      .OrderBy(r => r.MusteriAdi)
      .ThenBy(r => r.ParaBirimi)
      .ThenByDescending(r => r.NetPB)
      .ToList();


            // PB bazında alt toplamlar
            Totals = Items
                .GroupBy(i => i.ParaBirimi)
                .Select(g => new TotalRow
                {
                    ParaBirimi = g.Key,
                    BorcPB = g.Sum(x => x.BorcPB),
                    AlacakPB = g.Sum(x => x.AlacakPB)
                })
                .OrderBy(t => t.ParaBirimi)
                .ToList();

        }
    }
}
