// Pages/SeferMasraflari/Create.cshtml.cs
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

namespace Lojistik.Pages.SeferMasraflari
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? SeferSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }
            [Required, DataType(DataType.Date)] public DateTime Tarih { get; set; } = DateTime.Today;
            [Required, StringLength(50)] public string MasrafTipi { get; set; } = "Yakıt";
            [Required, Range(0, double.MaxValue)] public decimal Tutar { get; set; }
            [Required, StringLength(10)] public string ParaBirimi { get; set; } = "TRY";
            [StringLength(50)] public string? FaturaBelgeNo { get; set; }
            [StringLength(50)] public string? Ulke { get; set; }
            [StringLength(100)] public string? Yer { get; set; }
            [StringLength(300)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? seferId = null)
        {
            await LoadSelectsAsync(seferId);
            if (seferId.HasValue) Input.SeferID = seferId.Value;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SeferID);
                return Page();
            }

            var e = new SeferMasraf
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                SeferID = Input.SeferID,
                Tarih = Input.Tarih.Date,
                MasrafTipi = Input.MasrafTipi.Trim(),
                Tutar = Input.Tutar,
                ParaBirimi = Input.ParaBirimi.Trim(),
                FaturaBelgeNo = Input.FaturaBelgeNo?.Trim(),
                Ulke = Input.Ulke?.Trim(),
                Yer = Input.Yer?.Trim(),
                Notlar = Input.Notlar?.Trim()
            };

            _context.SeferMasraflari.Add(e);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Seferler/Details", new { id = e.SeferID });
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
