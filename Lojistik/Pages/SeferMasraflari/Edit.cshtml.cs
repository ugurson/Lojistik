// Pages/SeferMasraflari/Edit.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferMasraflari
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? SeferSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferMasrafID { get; set; }
            [Required] public int SeferID { get; set; }
            [Required, DataType(DataType.Date)] public DateTime Tarih { get; set; }
            [Required, StringLength(50)] public string MasrafTipi { get; set; } = null!;
            [Required, Range(0, double.MaxValue)] public decimal Tutar { get; set; }
            [Required, StringLength(10)] public string ParaBirimi { get; set; } = "TRY";
            [StringLength(50)] public string? FaturaBelgeNo { get; set; }
            [StringLength(50)] public string? Ulke { get; set; }
            [StringLength(100)] public string? Yer { get; set; }
            [StringLength(300)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.SeferMasraflari
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SeferMasrafID == id && x.FirmaID == firmaId);

            if (e == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SeferMasrafID = e.SeferMasrafID,
                SeferID = e.SeferID,
                Tarih = e.Tarih,
                MasrafTipi = e.MasrafTipi,
                Tutar = e.Tutar,
                ParaBirimi = e.ParaBirimi,
                FaturaBelgeNo = e.FaturaBelgeNo,
                Ulke = e.Ulke,
                Yer = e.Yer,
                Notlar = e.Notlar
            };

            await LoadSelectsAsync(e.SeferID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SeferID);
                return Page();
            }

            var e = await _context.SeferMasraflari
                .FirstOrDefaultAsync(x => x.SeferMasrafID == Input.SeferMasrafID && x.FirmaID == firmaId);

            if (e == null) return RedirectToPage("./Index");

            e.SeferID = Input.SeferID;
            e.Tarih = Input.Tarih.Date;
            e.MasrafTipi = Input.MasrafTipi.Trim();
            e.Tutar = Input.Tutar;
            e.ParaBirimi = Input.ParaBirimi.Trim();
            e.FaturaBelgeNo = Input.FaturaBelgeNo?.Trim();
            e.Ulke = Input.Ulke?.Trim();
            e.Yer = Input.Yer?.Trim();
            e.Notlar = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = e.SeferMasrafID });
        }

        private async Task LoadSelectsAsync(int? selected)
        {
            var firmaId = User.GetFirmaId();
            SeferSelect = new SelectList(
                await _context.Seferler.AsNoTracking()
                    .Where(s => s.FirmaID == firmaId)
                    .OrderByDescending(s => s.SeferID)
                    .Select(s => new { s.SeferID, Text = (s.SeferKodu ?? ("SF-" + s.SeferID)) + " | " + (s.Arac != null ? s.Arac.Plaka : "") })
                    .ToListAsync(),
                "SeferID", "Text", selected
            );
        }
    }
}
