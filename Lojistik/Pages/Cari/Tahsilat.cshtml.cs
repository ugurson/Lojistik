using System;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Cari
{
    public class TahsilatModel : PageModel
    {
        private readonly AppDbContext _context;
        public TahsilatModel(AppDbContext context) => _context = context;

        public class MusteriMini
        {
            public int MusteriID { get; set; }
            public string? MusteriAdi { get; set; }
        }

        [BindProperty] public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int MusteriID { get; set; }
            public DateTime Tarih { get; set; } = DateTime.Today;
            public string ParaBirimi { get; set; } = "TL";       // TL/EUR/USD...
            public decimal Tutar { get; set; }
            public decimal? Kur { get; set; }                     // Zorunlu değil
            public string? EvrakNo { get; set; }                  // Makbuz no vb.
            public string? Aciklama { get; set; }
        }

        public MusteriMini? SeciliMusteri { get; set; }

        public async Task<IActionResult> OnGetAsync(int? musteriId)
        {
            var firmaId = User.GetFirmaId();

            if (musteriId.HasValue)
            {
                SeciliMusteri = await _context.Musteriler.AsNoTracking()
                    .Where(m => m.MusteriID == musteriId.Value && m.FirmaID == firmaId)
                    .Select(m => new MusteriMini { MusteriID = m.MusteriID, MusteriAdi = m.MusteriAdi })
                    .FirstOrDefaultAsync();

                if (SeciliMusteri == null) return RedirectToPage("./Index");
                Input.MusteriID = SeciliMusteri.MusteriID;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (Input.MusteriID <= 0 || Input.Tutar <= 0 || string.IsNullOrWhiteSpace(Input.ParaBirimi))
            {
                ModelState.AddModelError("", "Zorunlu alanları kontrol edin.");
                return Page();
            }

            // SubeKodu: tahsilatta boş/NULL geçebilir (constraint için boşluk gönderme!)
            object? subeParam = null;

            var sql =
$@"
INSERT INTO dbo.CariHareketler
(
  FirmaID, SubeKodu, KullaniciID, MusteriID,
  IlgiliSiparisID, IlgiliSevkiyatID,
  Tarih,
  IslemTuru, EvrakNo, Aciklama,
  ParaBirimi, Yonu, Tutar, Kur, CreatedByKullaniciID
)
VALUES
(
  {{0}}, {{1}}, {{2}}, {{3}},
  NULL, NULL,
  {{4}},
  N'Tahsilat', {{5}}, {{6}},
  {{7}}, 0, {{8}}, {{9}}, {{10}}
);
";

            var parameters = new object?[]
            {
                firmaId,                       // 0 FirmaID
                subeParam,                     // 1 SubeKodu (NULL)
                userId,                        // 2 KullaniciID
                Input.MusteriID,               // 3 MusteriID
                Input.Tarih.Date,              // 4 Tarih
                string.IsNullOrWhiteSpace(Input.EvrakNo) ? null : Input.EvrakNo, // 5 EvrakNo
                string.IsNullOrWhiteSpace(Input.Aciklama) ? "Tahsilat" : Input.Aciklama!.Trim(), // 6 Açıklama
                Input.ParaBirimi.Trim().ToUpperInvariant(), // 7 PB
                Input.Tutar,                   // 8 Tutar
                Input.Kur,                     // 9 Kur (NULL olabilir)
                userId                         // 10 CreatedByKullaniciID
            };

            try
            {
                await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                TempData["StatusMessage"] = "Tahsilat kaydedildi.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Kayıt sırasında hata: " + (ex.InnerException?.Message ?? ex.Message));
                return Page();
            }
        }
    }
}
