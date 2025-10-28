using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lojistik.Pages.SeferMasraflari
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? ParaBirimleri { get; set; }
        public SelectList? MasrafTipleri { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }
            [DataType(DataType.Date)] public DateTime Tarih { get; set; } = DateTime.Today;
            [Required, StringLength(50)] public string MasrafTipi { get; set; } = "Yakıt";
            [Range(0, 999999999)] public decimal Tutar { get; set; }
            [Required, StringLength(10)] public string ParaBirimi { get; set; } = "EUR";
            [StringLength(50)] public string? FaturaBelgeNo { get; set; }
            [StringLength(50)] public string? Ulke { get; set; }
            [StringLength(100)] public string? Yer { get; set; }
            [StringLength(300)] public string? Notlar { get; set; }
        }

        public IActionResult OnGet(int seferId)
        {
            if (seferId <= 0) return RedirectToPage("/Seferler/Index");

            Input.SeferID = seferId;

            ParaBirimleri = new SelectList(new[] { "EUR", "USD", "TL" });
            MasrafTipleri = new SelectList(new[]
            {
                "Yakıt","Yakıt-Kapı","Şöför Fiks","Masraflar","Otoyol/Geçiş","Konaklama","Yemek","Bakım/Servis","Lastik",
                "Gümrük","Sigorta","Belge/Harç","Park","Diğer"
            });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var subeKodu = User.GetSubeKodu();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                ParaBirimleri = new SelectList(new[] { "EUR", "USD", "TL" });
                MasrafTipleri = new SelectList(new[] { "Yakıt", "Yakıt-Kapı", "Şöför Fiks","Masraflar", "Otoyol/Geçiş", "Konaklama", "Yemek", "Bakım/Servis", "Lastik", "Gümrük", "Sigorta", "Belge/Harç", "Park", "Diğer" });
                return Page();
            }

            var entity = new SeferMasraf
            {
                FirmaID = firmaId,
                SubeKodu = subeKodu,
                KullaniciID = userId,
                SeferID = Input.SeferID,
                Tarih = Input.Tarih,
                MasrafTipi = Input.MasrafTipi.Trim(),
                Tutar = Input.Tutar,
                ParaBirimi = Input.ParaBirimi.Trim(),
                FaturaBelgeNo = string.IsNullOrWhiteSpace(Input.FaturaBelgeNo) ? null : Input.FaturaBelgeNo.Trim(),
                Ulke = string.IsNullOrWhiteSpace(Input.Ulke) ? null : Input.Ulke.Trim(),
                Yer = string.IsNullOrWhiteSpace(Input.Yer) ? null : Input.Yer.Trim(),
                Notlar = string.IsNullOrWhiteSpace(Input.Notlar) ? null : Input.Notlar.Trim(),
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now
            };

            _context.SeferMasraflari.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Seferler/Details", new { id = Input.SeferID });
        }
    }
}
