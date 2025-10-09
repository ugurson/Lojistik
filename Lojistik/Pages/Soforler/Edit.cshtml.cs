using System;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // GetFirmaId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Soforler
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public Sofor Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var sofor = await _context.Soforler
                .FirstOrDefaultAsync(x => x.SoforID == id && x.FirmaID == firmaId);

            if (sofor == null) return NotFound();

            Input = sofor;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            if (!ModelState.IsValid) return Page();

            var db = await _context.Soforler
                .FirstOrDefaultAsync(x => x.SoforID == Input.SoforID && x.FirmaID == firmaId);

            if (db == null) return NotFound();

            // Güncellenecek alanlar
            db.AdSoyad = Input.AdSoyad;
            db.Telefon = Input.Telefon;
            db.Eposta = Input.Eposta;
            db.TCKimlikNo = Input.TCKimlikNo;
            db.Uyruk = Input.Uyruk;
            db.DogumTarihi = Input.DogumTarihi;
            db.KanGrubu = Input.KanGrubu;

            db.EhliyetNo = Input.EhliyetNo;
            db.EhliyetSinifi = Input.EhliyetSinifi;
            db.EhliyetVerilisTarihi = Input.EhliyetVerilisTarihi;
            db.EhliyetGecerlilikTarihi = Input.EhliyetGecerlilikTarihi;

            db.SRCBelgeNo = Input.SRCBelgeNo;
            db.PsikoteknikNo = Input.PsikoteknikNo;

            db.PasaportNo = Input.PasaportNo;
            db.PasaportBitisTarihi = Input.PasaportBitisTarihi;
            db.VizeBitisTarihi = Input.VizeBitisTarihi;

            db.SurucuKartNo = Input.SurucuKartNo;

            db.SGKNo = Input.SGKNo;
            db.IBAN = Input.IBAN;

            db.Notlar = Input.Notlar;
            db.Durum = Input.Durum;

            try
            {
                await _context.SaveChangesAsync();
                TempData["ok"] = "Şoför güncellendi.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Güncelleme hatası: " + ex.Message);
                return Page();
            }
        }
    }
}
