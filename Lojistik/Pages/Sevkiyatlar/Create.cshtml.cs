using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Sevkiyatlar
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? SiparisSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }
        public SelectList? YuklemeMusteriSelect { get; set; }
        public SelectList? BosaltmaMusteriSelect { get; set; }

        public class InputModel
        {
            [Required] public int SiparisID { get; set; }
            public int? DorseID { get; set; }

            public int? YuklemeMusteriID { get; set; }
            public int? BosaltmaMusteriID { get; set; }

            [StringLength(250)] public string? YuklemeAdres { get; set; }
            [StringLength(250)] public string? BosaltmaAdres { get; set; }
            [StringLength(200)] public string? YuklemeNoktasi { get; set; }
            [StringLength(200)] public string? BosaltmaNoktasi { get; set; }

            [DataType(DataType.Date)] public DateTime? PlanlananYuklemeTarihi { get; set; }
            public DateTime? YuklemeTarihi { get; set; }
            public DateTime? GumrukCikisTarihi { get; set; }
            public DateTime? VarisTarihi { get; set; }

            [StringLength(50)] public string? CMRNo { get; set; }
            [StringLength(50)] public string? MRN { get; set; }

            [Required] public byte Durum { get; set; } = 0;
            [StringLength(500)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? siparisId = null)
        {
            var firmaId = User.GetFirmaId();

            if (siparisId.HasValue)
            {
                var siparis = await _context.Siparisler
                    .Include(x => x.GonderenMusteri)
                    .Include(x => x.AliciMusteri)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SiparisID == siparisId.Value && x.FirmaID == firmaId);

                if (siparis == null) return NotFound();

                Input.SiparisID = siparis.SiparisID;
                Input.YuklemeMusteriID = siparis.GonderenMusteriID;
                Input.BosaltmaMusteriID = siparis.AliciMusteriID;
                Input.YuklemeAdres = siparis.GonderenMusteri?.Adres;
                Input.BosaltmaAdres = siparis.AliciMusteri?.Adres;

                var now = DateTime.Now;
                Input.PlanlananYuklemeTarihi = now;
                Input.YuklemeTarihi = now;
                Input.GumrukCikisTarihi = now;
                Input.VarisTarihi = now;
            }

            await LoadSelectsAsync(Input.SiparisID, Input.DorseID, Input.YuklemeMusteriID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SiparisID, Input.DorseID, Input.YuklemeMusteriID);
                return Page();
            }

            // --- Aynı sipariş için sevkiyat var mı kontrol et ---
            bool varMi = await _context.Sevkiyatlar
                .AnyAsync(x => x.FirmaID == firmaId && x.SiparisID == Input.SiparisID);
            if (varMi)
            {
                ModelState.AddModelError(string.Empty, "Bu sipariş için zaten bir sevkiyat oluşturulmuş.");
                await LoadSelectsAsync(Input.SiparisID, Input.DorseID, Input.YuklemeMusteriID);
                return Page();
            }

            // --- Yeni Sevkiyat kaydı ---
            var e = new Sevkiyat
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                SubeKodu = null,
                SiparisID = Input.SiparisID,
                DorseID = Input.DorseID,
                YuklemeMusteriID = Input.YuklemeMusteriID,
                BosaltmaMusteriID = Input.BosaltmaMusteriID,
                YuklemeAdres = Input.YuklemeAdres?.Trim(),
                BosaltmaAdres = Input.BosaltmaAdres?.Trim(),
                YuklemeNoktasi = Input.YuklemeNoktasi?.Trim(),
                BosaltmaNoktasi = Input.BosaltmaNoktasi?.Trim(),
                PlanlananYuklemeTarihi = Input.PlanlananYuklemeTarihi?.Date,
                YuklemeTarihi = Input.YuklemeTarihi,
                GumrukCikisTarihi = Input.GumrukCikisTarihi,
                VarisTarihi = Input.VarisTarihi,
                CMRNo = Input.CMRNo?.Trim(),
                MRN = Input.MRN?.Trim(),
                Durum = Input.Durum,
                Notlar = Input.Notlar?.Trim()
            };

            _context.Sevkiyatlar.Add(e);

            // --- Siparişin durumunu 1 (Onaylı) yap ---
            var siparis = await _context.Siparisler
                .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SiparisID == Input.SiparisID);
            if (siparis != null)
            {
                siparis.Durum = 1;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = e.SevkiyatID });
        }

        private async Task LoadSelectsAsync(int? siparisId, int? dorseId, int? yuklemeMusteriId)
        {
            var firmaId = User.GetFirmaId();

            SiparisSelect = new SelectList(
                await _context.Siparisler
                    .AsNoTracking()
                    .Where(x => x.FirmaID == firmaId)
                    .OrderByDescending(x => x.SiparisID)
                    .Select(x => new { x.SiparisID, Text = x.SiparisID + " - " + x.YukAciklamasi })
                    .ToListAsync(),
                "SiparisID", "Text", siparisId
            );

            DorselerSelect = new SelectList(
                await _context.Araclar
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && a.IsDorse == true)
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", dorseId
            );

            YuklemeMusteriSelect = new SelectList(
                await _context.Musteriler
                    .AsNoTracking()
                    .Where(m => m.FirmaID == firmaId)
                    .OrderBy(m => m.MusteriAdi)
                    .Select(m => new { m.MusteriID, m.MusteriAdi })
                    .ToListAsync(),
                "MusteriID", "MusteriAdi", yuklemeMusteriId
            );

            BosaltmaMusteriSelect = YuklemeMusteriSelect;
        }
    }
}
