// Pages/SeferSevkiyatlar/Create.cshtml.cs
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lojistik.Pages.SeferSevkiyatlar
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? SeferSelect { get; set; }
        public SelectList? SevkiyatSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }
            [Required] public int SevkiyatID { get; set; }
            [Required] public byte Yon { get; set; } = 0;
            [StringLength(300)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? seferId = null, int? sevkiyatId = null)
        {
            await LoadSelectsAsync(seferId, sevkiyatId);
            if (seferId.HasValue) Input.SeferID = seferId.Value;
            if (sevkiyatId.HasValue) Input.SevkiyatID = sevkiyatId.Value;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SeferID, Input.SevkiyatID);
                return Page();
            }

            var e = new SeferSevkiyat
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedAt = DateTime.Now,
                SeferID = Input.SeferID,
                SevkiyatID = Input.SevkiyatID,
                Yon = Input.Yon,
                Notlar = Input.Notlar?.Trim()
            };

            _context.SeferSevkiyatlar.Add(e);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Seferler/Details", new { id = e.SeferID });
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
