// Pages/SeferSevkiyatlar/Details.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferSevkiyatlar
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferSevkiyatID,
            int SeferID,
            string? SeferKodu,
            string? CekiciPlaka,
            int SevkiyatID,
            string? SevkiyatKodu,
            string? SevkiyatCekici,
            byte Yon,
            string? Notlar
        );

        public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .Where(x => x.SeferSevkiyatID == id && x.FirmaID == firmaId)
                .Select(x => new Item(
                    x.SeferSevkiyatID,
                    x.SeferID,
                    x.Sefer != null ? (x.Sefer.SeferKodu ?? ("SF-" + x.Sefer.SeferID)) : null,
                    x.Sefer != null && x.Sefer.Arac != null ? x.Sefer.Arac.Plaka : null,
                    x.SevkiyatID,
                    x.Sevkiyat != null ? (x.Sevkiyat.SevkiyatKodu ?? ("SV-" + x.Sevkiyat.SevkiyatID)) : null,
                    x.Sevkiyat != null && x.Sevkiyat.Arac != null ? x.Sevkiyat.Arac.Plaka : null,
                    x.Yon,
                    x.Notlar
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("/Seferler/Index");
            return Page();
        }
    }
}
