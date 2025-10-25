using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;                 // User.GetFirmaId(), GetUserId(), (varsa) GetSubeKodu()
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

        public SelectList? PBSelect { get; set; }
        public SelectList? SiparisSelect { get; set; }

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

            public int? IlgiliSiparisID { get; set; }

            [StringLength(300)]
            public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int seferId)
        {
            Input.SeferID = seferId;
            await LoadSelectsAsync(seferId, Input.ParaBirimi, null);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();
            // var sube   = User.GetSubeKodu(); // varsa kullan

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SeferID, Input.ParaBirimi, Input.IlgiliSiparisID);
                return Page();
            }

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
                IlgiliSiparisID = Input.IlgiliSiparisID,
                Notlar = string.IsNullOrWhiteSpace(Input.Notlar) ? null : Input.Notlar!.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.SeferGelirleri.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Seferler/Details", new { id = Input.SeferID });
        }

        private async Task LoadSelectsAsync(int seferId, string? selectedPB, int? selectedSiparisId)
        {
            PBSelect = new SelectList(new[]
            {
                new { Value = "TL",  Text = "TL - Türk Lirası" },
                new { Value = "EUR", Text = "EUR - Euro" },
                new { Value = "USD", Text = "USD - Amerikan Doları" }
            }, "Value", "Text", selectedPB);

            var firmaId = User.GetFirmaId();

            // Bu sefere bağlı siparişler (SeferSevkiyat → Sevkiyat → Siparis)
            var siparisler = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .Where(x => x.Sefer.FirmaID == firmaId && x.SeferID == seferId)
                .Select(x => new
                {
                    x.Sevkiyat.SiparisID,
                    Text = x.Sevkiyat.Siparis.SiparisID + " - " + x.Sevkiyat.Siparis.YukAciklamasi
                })
                .Distinct()
                .OrderBy(x => x.SiparisID)
                .ToListAsync();

            SiparisSelect = new SelectList(siparisler, "SiparisID", "Text", selectedSiparisId);
        }
    }
}
