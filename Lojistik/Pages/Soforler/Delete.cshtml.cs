using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Soforler
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        [BindProperty] public Sofor Data { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var sofor = await _context.Soforler.AsNoTracking().FirstOrDefaultAsync(x => x.SoforID == id && x.FirmaID == firmaId);
            if (sofor == null) return NotFound();
            Data = sofor;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var sofor = await _context.Soforler.FirstOrDefaultAsync(x => x.SoforID == id && x.FirmaID == firmaId);
            if (sofor == null) return NotFound();

            _context.Soforler.Remove(sofor);
            try
            {
                await _context.SaveChangesAsync();
                TempData["ok"] = "Şoför silindi.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                TempData["err"] = "Silme hatası: " + ex.Message;
                return RedirectToPage("Details", new { id });
            }
        }
    }
}
