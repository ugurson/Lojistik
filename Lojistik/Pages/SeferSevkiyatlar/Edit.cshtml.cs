// Pages/SeferSevkiyatlar/Edit.cshtml.cs
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferSevkiyatlar
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? SeferSelect { get; set; }
        public SelectList? SevkiyatSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferSevkiyatID { get; set; }
            [Required] public int SeferID { get; set; }
            [Required] public int SevkiyatID { get; set; }
            [Required] public byte Yon { get; set; }
            [StringLength(300)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SeferSevkiyatID == id && x.FirmaID == firmaId);

            if (e == null) return RedirectToPage("/Seferler/Index");

            Input = new InputModel
            {
                SeferSevkiyatID = e.SeferSevkiyatID,
                SeferID = e.SeferID,
                SevkiyatID = e.SevkiyatID,
                Yon = e.Yon,
                Notlar = e.Notlar
            };

            await LoadSelectsAsync(Input.SeferID, Input.SevkiyatID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SeferID, Input.SevkiyatID);
                return Page();
            }

            var e = await _context.SeferSevkiyatlar
                .FirstOrDefaultAsync(x => x.SeferSevkiyatID == Input.SeferSevkiyatID && x.FirmaID == firmaId);

            if (e == null) return RedirectToPage("/Seferler/Index");

            e.SeferID = Input.SeferID;
            e.SevkiyatID = Input.SevkiyatID;
            e.Yon = Input.Yon;
            e.Notlar = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = e.SeferSevkiyatID });
        }

        private async Task LoadSelectsAsync(int? seferId, int? sevkiyatId)
        {
            var firmaId = User.GetFirmaId();

            SeferSelect = new SelectList(
                await _context.Seferler.AsNoTracking()
                    .Where(s => s.FirmaID == firmaId)
                    .OrderByDescending(s => s.SeferID)
                    .Select(s => new { s.SeferID, Text = (s.SeferKodu ?? ("SF-" + s.SeferID)) + " | " + (s.Arac != null ? s.Arac.Plaka : "") })
                    .ToListAsync(),
                "SeferID", "Text", seferId
            );

            SevkiyatSelect = new SelectList(
                await _context.Sevkiyatlar.AsNoTracking()
                    .Where(s => s.FirmaID == firmaId)
                    .OrderByDescending(s => s.SevkiyatID)
                    .Select(s => new { s.SevkiyatID, Text = (s.SevkiyatKodu ?? ("SV-" + s.SevkiyatID)) + " | " + (s.Arac != null ? s.Arac.Plaka : "") })
                    .ToListAsync(),
                "SevkiyatID", "Text", sevkiyatId
            );
        }
    }
}
