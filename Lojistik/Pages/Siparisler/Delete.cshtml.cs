using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Siparis? Siparis { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Siparis = await _context.Siparisler
                .Include(s => s.GonderenMusteri)!.ThenInclude(m => m.Sehir)!.ThenInclude(se => se.Ulke)
                .Include(s => s.AliciMusteri)!.ThenInclude(m => m.Sehir)!.ThenInclude(se => se.Ulke)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SiparisID == id && s.FirmaID == firmaId);

            if (Siparis == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var siparis = await _context.Siparisler
                .FirstOrDefaultAsync(s => s.SiparisID == id && s.FirmaID == firmaId);

            if (siparis == null)
                return NotFound();

            try
            {
                _context.Siparisler.Remove(siparis);
                await _context.SaveChangesAsync();
                // Silindikten sonra listeye dön
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                // İlişkisel kısıt (FK) hatası vs. olabilir
                ModelState.AddModelError(string.Empty, "Kayıt silinemedi. Bu sipariş başka kayıtlarla ilişkili olabilir. Detay: " + ex.Message);
                // Kayıt yeniden yüklenip sayfa tekrar gösterilsin
                Siparis = await _context.Siparisler
                    .Include(s => s.GonderenMusteri)!.ThenInclude(m => m.Sehir)!.ThenInclude(se => se.Ulke)
                    .Include(s => s.AliciMusteri)!.ThenInclude(m => m.Sehir)!.ThenInclude(se => se.Ulke)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SiparisID == id && s.FirmaID == firmaId);

                return Page();
            }
        }
    }
}
