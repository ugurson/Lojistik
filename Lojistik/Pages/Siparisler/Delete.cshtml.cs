// Pages/Siparisler/Delete.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lojistik.Pages.Siparisler
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public record Item(
            int SiparisID,
            DateTime SiparisTarihi,
            string YukAciklamasi,
            string? Gonderen,
            string? Alici,
            decimal? Tutar,
            string? ParaBirimi,
            byte Durum
        );

        [BindProperty] public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SiparisID == id)
                .Select(s => new Item(
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.YukAciklamasi,
                    s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    s.Tutar,
                    s.ParaBirimi,
                    s.Durum
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.Siparisler
                .Include(s => s.Sevkiyatlar) // sadece sevkiyatları çekiyoruz
                .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SiparisID == id);

            if (e != null)
            {
                // Siparişe bağlı tüm sevkiyatları dolaş
                foreach (var sev in e.Sevkiyatlar.ToList())
                {
                    // Önce bu sevkiyata bağlı SeferSevkiyat kayıtlarını sil
                    var baglantilar = await _context.SeferSevkiyatlar
                        .Where(x => x.SevkiyatID == sev.SevkiyatID)
                        .ToListAsync();

                    if (baglantilar.Any())
                        _context.SeferSevkiyatlar.RemoveRange(baglantilar);

                    // Sonra sevkiyat kaydını sil
                    _context.Sevkiyatlar.Remove(sev);
                }

                // En son siparişi sil
                _context.Siparisler.Remove(e);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }

    }
}
