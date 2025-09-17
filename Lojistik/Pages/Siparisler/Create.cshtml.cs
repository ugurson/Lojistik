using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lojistik.Extensions; // User.GetFirmaId() vs için

namespace Lojistik.Pages.Siparisler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Siparis Siparis { get; set; } = new();

        // Dropdown verileri
        public SelectList? GonderenList { get; set; }
        public SelectList? AliciList { get; set; }
        public SelectList? AraTedarikciList { get; set; }
        public SelectList? ParaBirimiList { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Yeni Sipariş";   // garanti
            var firmaId = User.GetFirmaId();
            await HazirlaDropdownlarAsync(firmaId);

            Siparis.SiparisTarihi = System.DateTime.Today;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var subeKodu = User.GetSubeKodu();
            var userId = User.GetUserId();
            int? kullaniciId = userId == 0 ? (int?)null : userId;

            // Firma & kullanıcı bilgilerini set et
            Siparis.FirmaID = firmaId;
            Siparis.SubeKodu = subeKodu;
            Siparis.KullaniciID = kullaniciId;
            Siparis.CreatedByKullaniciID = kullaniciId;

            // Zorunlu alan validasyon
            if (Siparis.GonderenMusteriID <= 0)
                ModelState.AddModelError("Siparis.GonderenMusteriID", "Gönderen seçiniz.");
            if (Siparis.AliciMusteriID <= 0)
                ModelState.AddModelError("Siparis.AliciMusteriID", "Alıcı seçiniz.");

            if (!ModelState.IsValid)
            {
                await HazirlaDropdownlarAsync(firmaId);
                return Page();
            }

            _context.Siparisler.Add(Siparis);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Siparisler/Index");
        }

        private async Task HazirlaDropdownlarAsync(int firmaId)
        {
            // Senin DbSet adı -> Musteriler
            var musteriQuery = _context.Musteriler
                .Where(m => m.FirmaID == firmaId)
                .OrderBy(m => m.MusteriAdi)  // Eğer alan adı farklıysa (Ad/FirmaAdi) düzelt
                .Select(m => new { m.MusteriID, m.MusteriAdi });

            var list = await musteriQuery.AsNoTracking().ToListAsync();

            GonderenList = new SelectList(list, "MusteriID", "Unvan");
            AliciList = new SelectList(list, "MusteriID", "Unvan");
            AraTedarikciList = new SelectList(list, "MusteriID", "Unvan");

            ParaBirimiList = new SelectList(new[]
            {
                new { K = "TL",  V = "TL"  },
                new { K = "EUR", V = "EUR" },
                new { K = "USD", V = "USD" }
            }, "K", "V");
        }
    }
}
