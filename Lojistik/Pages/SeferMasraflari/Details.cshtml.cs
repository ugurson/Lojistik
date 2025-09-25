// Pages/SeferMasraflari/Details.cshtml.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferMasraflari
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferMasrafID,
            int SeferID,
            DateTime Tarih,
            string MasrafTipi,
            decimal Tutar,
            string ParaBirimi,
            string? FaturaBelgeNo,
            string? Ulke,
            string? Yer,
            string? Notlar,
            DateTime CreatedAt
        );

        public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.SeferMasraflari
                .AsNoTracking()
                .Where(x => x.SeferMasrafID == id && x.FirmaID == firmaId)
                .Select(x => new Item(
                    x.SeferMasrafID, x.SeferID, x.Tarih, x.MasrafTipi, x.Tutar, x.ParaBirimi,
                    x.FaturaBelgeNo, x.Ulke, x.Yer, x.Notlar, x.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }
    }
}
