// Pages/SeferMasraflari/Delete.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lojistik.Pages.SeferMasraflari
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferMasrafID,
            int SeferID,
            DateTime Tarih,
            string MasrafTipi,
            decimal Tutar,
            string ParaBirimi
        );

        [BindProperty] public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.SeferMasraflari.AsNoTracking()
                .Where(x => x.SeferMasrafID == id && x.FirmaID == firmaId)
                .Select(x => new Item(x.SeferMasrafID, x.SeferID, x.Tarih, x.MasrafTipi, x.Tutar, x.ParaBirimi))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.SeferMasraflari
                .FirstOrDefaultAsync(x => x.SeferMasrafID == id && x.FirmaID == firmaId);

            if (e != null)
            {
                var seferId = e.SeferID;
                _context.SeferMasraflari.Remove(e);
                await _context.SaveChangesAsync();
                return RedirectToPage("/Seferler/Details", new { id = seferId });
            }

            return RedirectToPage("./Index");
        }
    }
}
