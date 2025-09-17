using System.ComponentModel.DataAnnotations;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()...
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Firma bazlı müşteri listesi (dropdown)
        public SelectList? MusterilerSelect { get; set; }

        public class InputModel
        {
            [Required]
            public int SiparisID { get; set; }

            [Required]
            [DataType(DataType.Date)]
            public DateTime SiparisTarihi { get; set; }

            // NOT: Entity tarafı int (nullable değil) ise burada da int + [Required]
            [Required, Display(Name = "Gönderen Müşteri")]
            public int GonderenMusteriID { get; set; }

            [Required, Display(Name = "Alıcı Müşteri")]
            public int AliciMusteriID { get; set; }

            [Display(Name = "Ara Tedarikçi")]
            public int? AraTedarikciMusteriID { get; set; }

            [Required, StringLength(500)]
            public string YukAciklamasi { get; set; } = string.Empty;

            public int? Kilo { get; set; }

            [DataType(DataType.Currency)]
            public decimal? Tutar { get; set; }

            [StringLength(3)]
            public string? ParaBirimi { get; set; } // "TRY","EUR","USD" vb.

            [Display(Name = "Fatura No")]
            [StringLength(50)]
            public string? FaturaNo { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var s = await _context.Siparisler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SiparisID == id && x.FirmaID == firmaId);

            if (s is null) return NotFound();

            Input = new InputModel
            {
                SiparisID = s.SiparisID,
                SiparisTarihi = s.SiparisTarihi,
                GonderenMusteriID = s.GonderenMusteriID,   // int -> int
                AliciMusteriID = s.AliciMusteriID,         // int -> int
                AraTedarikciMusteriID = s.AraTedarikciMusteriID,
                YukAciklamasi = s.YukAciklamasi,
                Kilo = s.Kilo,
                Tutar = s.Tutar,
                ParaBirimi = s.ParaBirimi,
                FaturaNo = s.FaturaNo
            };

            await LoadMusterilerAsync(firmaId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadMusterilerAsync(firmaId);
                return Page();
            }

            var s = await _context.Siparisler
                .FirstOrDefaultAsync(x => x.SiparisID == Input.SiparisID && x.FirmaID == firmaId);

            if (s is null) return NotFound();

            // Sadece izin verilen alanları güncelle
            s.SiparisTarihi = Input.SiparisTarihi;
            s.GonderenMusteriID = Input.GonderenMusteriID;   // int -> int (hata yok)
            s.AliciMusteriID = Input.AliciMusteriID;         // int -> int (hata yok)
            s.AraTedarikciMusteriID = Input.AraTedarikciMusteriID;
            s.YukAciklamasi = Input.YukAciklamasi;
            s.Kilo = Input.Kilo;
            s.Tutar = Input.Tutar;
            s.ParaBirimi = Input.ParaBirimi;
            s.FaturaNo = Input.FaturaNo;

            try
            {
                await _context.SaveChangesAsync();
                // Düzenleme sonrası Detay'a yönlendirelim
                return RedirectToPage("./Details", new { id = s.SiparisID });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Kayıt başka biri tarafından değiştirildi. Lütfen sayfayı yenileyip tekrar deneyin.");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu: " + ex.Message);
            }

            await LoadMusterilerAsync(firmaId);
            return Page();
        }

        private async Task LoadMusterilerAsync(int firmaId)
        {
            var list = await _context.Musteriler
                .Where(m => m.FirmaID == firmaId)
                .OrderBy(m => m.MusteriAdi)
                .Select(m => new { m.MusteriID, m.MusteriAdi })
                .ToListAsync();

            MusterilerSelect = new SelectList(list, "MusteriID", "MusteriAdi");
        }
    }
}
