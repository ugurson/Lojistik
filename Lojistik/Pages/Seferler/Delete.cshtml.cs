// Pages/Seferler/Delete.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lojistik.Pages.Seferler
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferID,
            string? SeferKodu,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi,
            byte Durum
        );

        [BindProperty] public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SeferID == id)
                .Select(s => new Item(
                    s.SeferID,
                    s.SeferKodu,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.CikisTarihi,
                    s.DonusTarihi,
                    s.Durum
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.Seferler
                .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SeferID == id);

            if (e != null)
            {
                _context.Seferler.Remove(e);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
