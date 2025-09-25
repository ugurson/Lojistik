// Pages/SeferGelirleri/Edit.cshtml.cs
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

namespace Lojistik.Pages.SeferGelirleri
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? SeferSelect { get; set; }
        public SelectList? SiparisSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferGelirID { get; set; }
            [Required] public int SeferID { get; set; }

            [Required, DataType(DataType.Date)]
            public DateTime Tarih { get; set; }

            [StringLength(200)] public string? Aciklama { get; set; }

            [Required, Range(0, double.MaxValue)]
            public decimal Tutar { get; set; }

            [Required, StringLength(10)]
            public string ParaBirimi { get; set; } = "TRY";

            public int? IlgiliSiparisID { get; set; }

            [StringLength(300)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var g = await _context.SeferGelirleri
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SeferGelirID == id && x.FirmaID == firmaId);

            if (g == null) return RedirectToPage("/Seferler/Index");

            Input = new InputModel
            {
                SeferGelirID = g.SeferGelirID,
                SeferID = g.SeferID,
                Tarih = g.Tarih,
                Aciklama = g.Aciklama,
                Tutar = g.Tutar,
                ParaBirimi = g.ParaBirimi,
                IlgiliSiparisID = g.IlgiliSiparisID,
                Notlar = g.Notlar
            };

            await LoadSelectsAsync(g.SeferID, g.IlgiliSiparisID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SeferID, Input.IlgiliSiparisID);
                return Page();
            }

            var g = await _context.SeferGelirleri
                .FirstOrDefaultAsync(x => x.SeferGelirID == Input.SeferGelirID && x.FirmaID == firmaId);

            if (g == null) return RedirectToPage("/Seferler/Index");

            g.SeferID = Input.SeferID;
            g.Tarih = Input.Tarih.Date;
            g.Aciklama = Input.Aciklama?.Trim();
            g.Tutar = Input.Tutar;
            g.ParaBirimi = Input.ParaBirimi.Trim();
            g.IlgiliSiparisID = Input.IlgiliSiparisID;
            g.Notlar = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = g.SeferGelirID });
        }

        private async Task LoadSelectsAsync(int? seferId, int? siparisId)
        {
            var firmaId = User.GetFirmaId();

            SeferSelect = new SelectList(
                await _context.Seferler
                    .AsNoTracking()
                    .Where(s => s.FirmaID == firmaId)
                    .OrderByDescending(s => s.SeferID)
                    .Select(s => new { s.SeferID, Text = (s.SeferKodu ?? ("SF-" + s.SeferID)) + " | " + (s.Arac != null ? s.Arac.Plaka : "") })
                    .ToListAsync(),
                "SeferID", "Text", seferId
            );

            SiparisSelect = new SelectList(
                await _context.Siparisler
                    .AsNoTracking()
                    .Where(x => x.FirmaID == firmaId)
                    .OrderByDescending(x => x.SiparisID)
                    .Select(x => new { x.SiparisID, Text = x.SiparisID + " - " + x.YukAciklamasi })
                    .ToListAsync(),
                "SiparisID", "Text", siparisId
            );
        }
    }
}
