using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Seferler
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Sefer? Sefer { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Sefer = await _context.Seferler
                .AsNoTracking()
                .Include(s => s.Arac)
                .Include(s => s.Dorse)
                .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SeferID == id);

            if (Sefer == null)
                return RedirectToPage("./Index");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.Seferler
                .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SeferID == id);

            if (e != null)
            {
                // --- Bağlantıları al ---
                var baglantilar = await _context.SeferSevkiyatlar
                    .Where(x => x.SeferID == e.SeferID && x.FirmaID == firmaId)
                    .ToListAsync();

                if (baglantilar.Any())
                {
                    // --- Bağlı siparişlerin durumunu 1 yap ---
                    var siparisIds = await _context.Sevkiyatlar
                        .Where(sv => baglantilar.Select(b => b.SevkiyatID).Contains(sv.SevkiyatID))
                        .Select(sv => sv.SiparisID)
                        .ToListAsync();

                    var siparisler = await _context.Siparisler
                        .Where(sp => siparisIds.Contains(sp.SiparisID))
                        .ToListAsync();

                    foreach (var sp in siparisler)
                        sp.Durum = 1;

                    // --- SeferSevkiyat bağlantılarını sil ---
                    _context.SeferSevkiyatlar.RemoveRange(baglantilar);
                }

                // --- Seferi sil ---
                _context.Seferler.Remove(e);

                // ✅ Tek seferde kaydet
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
