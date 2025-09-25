// Pages/SeferGelirleri/Create.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferGelirleri
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? SeferSelect { get; set; }
        public SelectList? SiparisSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }

            [Required, DataType(DataType.Date)]
            public DateTime Tarih { get; set; } = DateTime.Today;

            [StringLength(200)] public string? Aciklama { get; set; }

            [Required, Range(0, double.MaxValue)]
            public decimal Tutar { get; set; }

            [Required, StringLength(10)]
            public string ParaBirimi { get; set; } = "TRY";

            public int? IlgiliSiparisID { get; set; }

            [StringLength(300)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? seferId = null, int? siparisId = null)
        {
            await LoadSelectsAsync(seferId, siparisId);
            if (seferId.HasValue) Input.SeferID = seferId.Value;
            if (siparisId.HasValue) Input.IlgiliSiparisID = siparisId.Value;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SeferID, Input.IlgiliSiparisID);
                return Page();
            }

            var e = new SeferGelir
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedAt = DateTime.Now,

                SeferID = Input.SeferID,
                Tarih = Input.Tarih.Date,
                Aciklama = Input.Aciklama?.Trim(),
                Tutar = Input.Tutar,
                ParaBirimi = Input.ParaBirimi.Trim(),
                IlgiliSiparisID = Input.IlgiliSiparisID,
                Notlar = Input.Notlar?.Trim()
            };

            _context.SeferGelirleri.Add(e);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Seferler/Details", new { id = e.SeferID });
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
