// Pages/SeferGelirleri/Delete.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lojistik.Pages.SeferGelirleri
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferGelirID,
            int SeferID,
            string? SeferKodu,
            string? CekiciPlaka,
            DateTime Tarih,
            string? Aciklama,
            decimal Tutar,
            string ParaBirimi,
            int? IlgiliSiparisID
        );

        [BindProperty] public Item? Data { get; set; }

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
                    g.IlgiliSiparisID
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("/Seferler/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.SeferGelirleri
                .FirstOrDefaultAsync(g => g.SeferGelirID == id && g.FirmaID == firmaId);

            if (e != null)
            {
                var seferId = e.SeferID;
                _context.SeferGelirleri.Remove(e);
                await _context.SaveChangesAsync();
                return RedirectToPage("/Seferler/Details", new { id = seferId });
            }

            return RedirectToPage("/Seferler/Index");
        }
    }
}
