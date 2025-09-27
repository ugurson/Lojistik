using System.ComponentModel.DataAnnotations;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Sevkiyatlar
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public SelectList? DorselerSelect { get; set; }

        public class InputModel
        {
            public int SevkiyatID { get; set; }
            public int? DorseID { get; set; }
            [DataType(DataType.Date)] public DateTime? PlanlananYuklemeTarihi { get; set; }
            public DateTime? YuklemeTarihi { get; set; }
            public DateTime? VarisTarihi { get; set; }
            public byte Durum { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.Sevkiyatlar
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == id);

            if (e == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SevkiyatID = e.SevkiyatID,
                DorseID = e.DorseID,
                PlanlananYuklemeTarihi = e.PlanlananYuklemeTarihi,
                YuklemeTarihi = e.YuklemeTarihi,
                VarisTarihi = e.VarisTarihi,
                Durum = e.Durum
            };

            await LoadSelectsAsync(firmaId, Input.DorseID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(firmaId, Input.DorseID);
                return Page();
            }

            var e = await _context.Sevkiyatlar
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == Input.SevkiyatID);

            if (e == null) return RedirectToPage("./Index");

            e.DorseID = Input.DorseID;
            e.PlanlananYuklemeTarihi = Input.PlanlananYuklemeTarihi;
            e.YuklemeTarihi = Input.YuklemeTarihi;
            e.VarisTarihi = Input.VarisTarihi;
            e.Durum = Input.Durum;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = e.SevkiyatID });
        }

        private async Task LoadSelectsAsync(int firmaId, int? dorseId)
        {
            DorselerSelect = new SelectList(
                await _context.Araclar
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && a.IsDorse == true)
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", dorseId
            );
        }
    }
}
