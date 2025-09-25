// Pages/SeferMasraflari/Index.cshtml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferMasraflari
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SeferMasrafID,
            int SeferID,
            DateTime Tarih,
            string MasrafTipi,
            decimal Tutar,
            string ParaBirimi,
            string? Yer,
            string? Ulke
        );

        public IList<Row> Items { get; set; } = new List<Row>();

        public async Task OnGetAsync(int? seferId = null)
        {
            var firmaId = User.GetFirmaId();

            var q = _context.SeferMasraflari.AsNoTracking()
                .Where(x => x.FirmaID == firmaId);

            if (seferId.HasValue) q = q.Where(x => x.SeferID == seferId.Value);

            Items = await q
                .OrderByDescending(x => x.Tarih).ThenByDescending(x => x.SeferMasrafID)
                .Select(x => new Row(
                    x.SeferMasrafID, x.SeferID, x.Tarih, x.MasrafTipi, x.Tutar, x.ParaBirimi, x.Yer, x.Ulke
                ))
                .ToListAsync();
        }
    }
}
