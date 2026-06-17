using System.Security.Claims;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Musteriler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public IList<Musteri> List { get; set; } = new List<Musteri>();

        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public byte? kategori { get; set; }

        public async Task OnGetAsync()
        {
            var firmaIdStr = User.FindFirstValue("FirmaID");
            if (!int.TryParse(firmaIdStr, out var firmaId))
            {
                List = new List<Musteri>();
                return;
            }

            var query = _context.Musteriler
                .Include(m => m.Ulke)
                .Include(m => m.Sehir)
                .Where(m => m.FirmaID == firmaId)
                .AsQueryable();

            if (kategori.HasValue)
                query = query.Where(m => m.Kategori == kategori.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(m => m.MusteriAdi.ToLower().Contains(term));
            }

            List = await query.OrderBy(m => m.Kategori).ThenBy(m => m.MusteriAdi).ToListAsync();
        }
    }
}
