// Pages/Seferler/Details.cshtml.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Seferler
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public record Item(
            int SeferID,
            string? SeferKodu,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi,
            byte Durum,
            string? Notlar,
            DateTime CreatedAt
        );

        public Item? Data { get; set; }

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
                    s.Durum,
                    s.Notlar,
                    s.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }
    }
}
