using System;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // GetFirmaId(), GetSubeKodu(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lojistik.Pages.Soforler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Sofor Input { get; set; } = new();

        public void OnGet()
        {
            // 🔹 Tarih alanlarını bugünün tarihi ile doldur
            var today = DateTime.Today;

            Input.DogumTarihi = today;
            Input.EhliyetVerilisTarihi = today;
            Input.EhliyetGecerlilikTarihi = today;
            Input.PasaportBitisTarihi = today;
            Input.VizeBitisTarihi = today;

            // İstersen yalnızca ehliyet/pasaport/vizeyi bugün yap, doğumu boş bırak:
            // Input.DogumTarihi = null;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            Input.FirmaID = User.GetFirmaId();
            Input.SubeKodu = User.GetSubeKodu();
            Input.CreatedByKullaniciID = User.GetUserId();

            _context.Soforler.Add(Input);

            try
            {
                await _context.SaveChangesAsync();
                TempData["ok"] = "Şoför eklendi.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Kayıt sırasında hata: " + ex.Message);
                return Page();
            }
        }
    }
}
