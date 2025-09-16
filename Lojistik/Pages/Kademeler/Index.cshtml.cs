using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Kademeler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public IList<AracKademe> Kademeler { get; set; } = new List<AracKademe>();

        [BindProperty(SupportsGet = true)] public int? AracID { get; set; } // [YENİ]
        public string? Plaka { get; set; } // [YENİ]

        public async Task OnGetAsync(int? aracId, string? plaka)
        {
            AracID = aracId ?? AracID;
            Plaka = plaka;

            IQueryable<AracKademe> q = _context.AracKademeler
                .Include(k => k.Arac)
                .OrderByDescending(k => k.Tarih);

            if (AracID.HasValue)
            {
                q = q.Where(k => k.AracID == AracID.Value);
                if (string.IsNullOrWhiteSpace(Plaka))
                {
                    Plaka = await _context.Araclar
                        .Where(a => a.AracID == AracID.Value)
                        .Select(a => a.Plaka)
                        .FirstOrDefaultAsync();
                }
            }

            Kademeler = await q.ToListAsync();
        }
    }
}
