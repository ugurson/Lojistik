using Lojistik.Data;
using Lojistik.Extensions; // GetFirmaId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Belgeler
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        [BindProperty]
        public AracBelgesi AracBelgesi { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int firmaId = User.GetFirmaId();

            AracBelgesi = await _context.AracBelgeleri
                .Include(b => b.Arac)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BelgeID == id && b.Arac.FirmaID == firmaId);

            if (AracBelgesi == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            int firmaId = User.GetFirmaId();

            // Güvenli: belgeyi firma filtresiyle çek
            var belge = await _context.AracBelgeleri
                .Join(_context.Araclar, b => b.AracID, a => a.AracID, (b, a) => new { b, a })
                .Where(x => x.b.BelgeID == id && x.a.FirmaID == firmaId)
                .Select(x => x.b)
                .FirstOrDefaultAsync();

            if (belge == null) return NotFound();

            var aracId = belge.AracID; // redirect için sakla

            _context.AracBelgeleri.Remove(belge);
            await _context.SaveChangesAsync();
            TempData["Msg"] = "Belge başarıyla silindi.";
            // İSTENEN: her zaman o aracın detayına dön
            return RedirectToPage("/Araclar/Details", new { id = aracId });
        }
    }
}
