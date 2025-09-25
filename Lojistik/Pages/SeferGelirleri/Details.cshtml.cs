// Pages/SeferGelirleri/Details.cshtml.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferGelirleri
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferGelirID,
            int SeferID,
            string? SeferKodu,
            string? CekiciPlaka,
            DateTime Tarih,
            string? Aciklama,
            decimal Tutar,
            string ParaBirimi,
            int? IlgiliSiparisID,
            string? Notlar,
            DateTime CreatedAt
        );

        public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.SeferGelirID == id && g.FirmaID == firmaId)
                .Select(g => new Item(
                    g.SeferGelirID,
                    g.SeferID,
                    g.Sefer != null ? g.Sefer.SeferKodu : null,
                    g.Sefer != null && g.Sefer.Arac != null ? g.Sefer.Arac.Plaka : null,
                    g.Tarih,
                    g.Aciklama,
                    g.Tutar,
                    g.ParaBirimi,
                    g.IlgiliSiparisID,
                    g.Notlar,
                    g.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("/Seferler/Index");
            return Page();
        }
    }
}
