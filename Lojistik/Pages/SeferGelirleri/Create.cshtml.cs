using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;                 // User.GetFirmaId(), GetUserId(), (varsa) GetSubeKodu()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lojistik.Pages.SeferGelirleri
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? PBSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }

            [DataType(DataType.Date)]
            public DateTime Tarih { get; set; } = DateTime.Today;

            [StringLength(200)]
            public string? Aciklama { get; set; } = "Navlun";

            [Required]
            [Range(typeof(decimal), "0", "9999999999999,99", ErrorMessage = "Geçersiz tutar.")]
            public decimal Tutar { get; set; }

            [Required, StringLength(10)]
            public string ParaBirimi { get; set; } = "TL";

            [StringLength(300)]
            public string? Notlar { get; set; }
            [StringLength(100)]
            public string? CikisIl { get; set; }

            [StringLength(100)]
            public string? VarisIl { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int seferId)
        {
            Input.SeferID = seferId;
            await LoadSelectsAsync(Input.ParaBirimi);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();
            // var sube   = User.GetSubeKodu(); // varsa kullan

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.ParaBirimi);
                return Page();
            }

            var seferAit = await _context.Seferler
                .AnyAsync(s => s.FirmaID == firmaId && s.SeferID == Input.SeferID);
            if (!seferAit) return Forbid();

            var entity = new SeferGelir
            {
                FirmaID = firmaId,
                // SubeKodu = sube,
                KullaniciID = userId,
                SeferID = Input.SeferID,
                Tarih = Input.Tarih.Date,
                Aciklama = string.IsNullOrWhiteSpace(Input.Aciklama) ? null : Input.Aciklama!.Trim(),
                Tutar = Input.Tutar,
                ParaBirimi = Input.ParaBirimi,
                Notlar = string.IsNullOrWhiteSpace(Input.Notlar) ? null : Input.Notlar!.Trim(),
                CikisIl = string.IsNullOrWhiteSpace(Input.CikisIl) ? null : Input.CikisIl!.Trim(),
                VarisIl = string.IsNullOrWhiteSpace(Input.VarisIl) ? null : Input.VarisIl!.Trim(),

                CreatedAt = DateTime.Now
            };

            _context.SeferGelirleri.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Seferler/Details", new { id = Input.SeferID });
        }

        private Task LoadSelectsAsync(string? selectedPB)
        {
            PBSelect = new SelectList(new[]
            {
                new { Value = "TL",  Text = "TL - Türk Lirası" },
                new { Value = "EUR", Text = "EUR - Euro" },
                new { Value = "USD", Text = "USD - Amerikan Doları" }
            }, "Value", "Text", selectedPB);
            return Task.CompletedTask;
        }
    }
}
