// Pages/SeferSevkiyatlar/Delete.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferSevkiyatlar
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferSevkiyatID,
            int SeferID,
            string? SeferKodu,
            int SevkiyatID,
            string? SevkiyatKodu,
            byte Yon,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi
        );

        [BindProperty] public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.SeferSevkiyatID == id)
                .Select(x => new Item(
                    x.SeferSevkiyatID,
                    x.SeferID,
                    x.Sefer != null ? x.Sefer.SeferKodu : null,
                    x.SevkiyatID,
                    x.Sevkiyat != null ? x.Sevkiyat.SevkiyatKodu : null,
                    x.Yon,
                    x.Sevkiyat != null && x.Sevkiyat.Arac != null ? x.Sevkiyat.Arac.Plaka : null,
                    x.Sevkiyat != null && x.Sevkiyat.Dorse != null ? x.Sevkiyat.Dorse.Plaka : null,
                    x.Sevkiyat != null ? x.Sevkiyat.SurucuAdi : null
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("/Seferler/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var entity = await _context.SeferSevkiyatlar
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SeferSevkiyatID == id);

            if (entity != null)
            {
                var seferId = entity.SeferID;
                _context.SeferSevkiyatlar.Remove(entity);
                await _context.SaveChangesAsync();
                return RedirectToPage("/Seferler/Details", new { id = seferId });
            }

            return RedirectToPage("/Seferler/Index");
        }
    }
}
