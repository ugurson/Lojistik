using System;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Seferler
{
    public class AddSiparisModel : PageModel
    {
        private readonly AppDbContext _context;
        public AddSiparisModel(AppDbContext context) => _context = context;

        [BindProperty] public int SiparisID { get; set; }
        public SelectList? SiparislerSelect { get; set; }
        public int SeferID { get; set; }

        public async Task<IActionResult> OnGetAsync(int seferId)
        {
            var firmaId = User.GetFirmaId();
            SeferID = seferId;

            var siparisler = await _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.Durum == 1) // sadece onaylı siparişler
                .Select(s => new
                {
                    s.SiparisID,
                    // dorse plakası → sevkiyat üzerinden
                    DorsePlaka = s.Sevkiyatlar
                        .OrderByDescending(v => v.SevkiyatID)
                        .Select(v => v.Dorse != null ? v.Dorse.Plaka : "(Dorse Yok)")
                        .FirstOrDefault(),
                    // alıcı
                    Alici = s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : "-",
                    // ülke
                    Ulke = s.AliciMusteri != null && s.AliciMusteri.Sehir != null
                           ? s.AliciMusteri.Sehir.Ulke.UlkeAdi
                           : "-"
                })
                .ToListAsync();

            // ✅ SelectList oluştururken Text alanını direkt formatlıyoruz
            SiparislerSelect = new SelectList(
                siparisler.Select(x => new
                {
                    x.SiparisID,
                    Text = $"{x.SiparisID} - {x.DorsePlaka} - {x.Alici} ({x.Ulke})"
                }),
                "SiparisID", "Text"
            );

            return Page();
        }


        public async Task<IActionResult> OnPostAsync(int seferId)
        {
            var firmaId = User.GetFirmaId();

            if (SiparisID == 0)
                return Page();

            // ✅ Seferi bul
            var sefer = await _context.Seferler
                .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SeferID == seferId);

            if (sefer == null)
                return NotFound();

            // ✅ Daha önce bu sipariş zaten eklenmiş mi?
            var exists = await _context.SeferSevkiyatlar
                .AnyAsync(x => x.SeferID == seferId && x.Sevkiyat.SiparisID == SiparisID);

            if (!exists)
            {
                // ✅ Önce sevkiyat bul
                var sevkiyatId = await _context.Sevkiyatlar
                    .Where(s => s.FirmaID == firmaId && s.SiparisID == SiparisID)
                    .Select(s => s.SevkiyatID)
                    .FirstOrDefaultAsync();

                if (sevkiyatId != 0)
                {
                    // ✅ SeferSevkiyat bağlantısını ekle
                    var baglanti = new SeferSevkiyat
                    {
                        FirmaID = firmaId,
                        SeferID = seferId,
                        SevkiyatID = sevkiyatId
                    };

                    _context.SeferSevkiyatlar.Add(baglanti);

                    // ✅ Bağlı siparişin durumunu 2 yap
                    var siparis = await _context.Siparisler
                        .FirstOrDefaultAsync(s => s.SiparisID == SiparisID && s.FirmaID == firmaId);

                    if (siparis != null)
                        siparis.Durum = 2;

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage("./Details", new { id = seferId });
        }

    }
}
