// Pages/Seferler/Create.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lojistik.Pages.Seferler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? CekicilerSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }

        public class InputModel
        {
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

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectsAsync(null, null);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.AracID, Input.DorseID);
                return Page();
            }

            var e = new Sefer
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                SubeKodu = null,
                AracID = Input.AracID,
                DorseID = Input.DorseID,
                SurucuID = Input.SurucuID,
                SurucuAdi = Input.SurucuAdi?.Trim(),
                SeferKodu = Input.SeferKodu?.Trim(),
                CikisTarihi = Input.CikisTarihi,
                DonusTarihi = Input.DonusTarihi,
                Durum = Input.Durum,
                Notlar = Input.Notlar?.Trim()
            };

            _context.Seferler.Add(e);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = e.SeferID });
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
