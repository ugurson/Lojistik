using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lojistik.Extensions;

namespace Lojistik.Pages.Siparisler
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public Siparis Siparis { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Siparis = await _context.Siparisler
                .Include(s => s.GonderenMusteri)!.ThenInclude(m => m.Sehir)!.ThenInclude(se => se.Ulke)
                .Include(s => s.AliciMusteri)!.ThenInclude(m => m.Sehir)!.ThenInclude(se => se.Ulke)
                .FirstOrDefaultAsync(s => s.SiparisID == id && s.FirmaID == firmaId);

            if (Siparis == null)
                return NotFound();

            return Page();
        }
    }
}
