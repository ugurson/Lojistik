// Pages/Seferler/Edit.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lojistik.Pages.Seferler
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? CekicilerSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }

            [StringLength(30)] public string? SeferKodu { get; set; }

            [Required] public int AracID { get; set; }
            public int? DorseID { get; set; }

            public int? SurucuID { get; set; }
            [StringLength(100)] public string? SurucuAdi { get; set; }

            public DateTime? CikisTarihi { get; set; }
            public DateTime? DonusTarihi { get; set; }

            [Required] public byte Durum { get; set; } = 0;
            [StringLength(500)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var s = await _context.Seferler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SeferID == id);

            if (s == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SeferID = s.SeferID,
                SeferKodu = s.SeferKodu,
                AracID = s.AracID,
                DorseID = s.DorseID,
                SurucuID = s.SurucuID,
                SurucuAdi = s.SurucuAdi,
                CikisTarihi = s.CikisTarihi,
                DonusTarihi = s.DonusTarihi,
                Durum = s.Durum,
                Notlar = s.Notlar
            };

            await LoadSelectsAsync(Input.AracID, Input.DorseID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.AracID, Input.DorseID);
                return Page();
            }

            var s = await _context.Seferler
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SeferID == Input.SeferID);

            if (s == null) return RedirectToPage("./Index");

            s.SeferKodu = Input.SeferKodu?.Trim();
            s.AracID = Input.AracID;
            s.DorseID = Input.DorseID;
            s.SurucuID = Input.SurucuID;
            s.SurucuAdi = Input.SurucuAdi?.Trim();
            s.CikisTarihi = Input.CikisTarihi;
            s.DonusTarihi = Input.DonusTarihi;
            s.Durum = Input.Durum;
            s.Notlar = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = s.SeferID });
        }

        private async Task LoadSelectsAsync(int? cekiciId, int? dorseId)
        {
            var firmaId = User.GetFirmaId();

            CekicilerSelect = new SelectList(
                await _context.Araclar
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && (a.IsDorse == false || a.IsDorse == null))
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", cekiciId
            );

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
