using System.Security.Claims;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Musteriler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        // Razor tarafında Model.List ile kullanıyoruz
        public IList<Musteri> List { get; set; } = new List<Musteri>();

        public async Task OnGetAsync()
        {
            // FirmaID claim'i ile multi-tenant filtre
            var firmaIdStr = User.FindFirstValue("FirmaID");
            if (!int.TryParse(firmaIdStr, out var firmaId))
            {
                List = new List<Musteri>();
                return;
            }

            List = await _context.Musteriler
                .Include(m => m.Ulke)
                .Include(m => m.Sehir)
                .Where(m => m.FirmaID == firmaId)
                .OrderBy(m => m.MusteriAdi)
                .ToListAsync();
        }
    }
}
