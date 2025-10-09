using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace Lojistik.Pages.Sevkiyatlar
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public CreateModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? SiparisSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }
        public SelectList? YuklemeMusteriSelect { get; set; }
        public SelectList? BosaltmaMusteriSelect { get; set; }

        public class InputModel
        {
            [Required] public int SiparisID { get; set; }

            [Required(ErrorMessage = "Dorse seçiniz.")]
            public int? DorseID { get; set; }

            [Required] public int YuklemeMusteriID { get; set; }
            [Required] public int BosaltmaMusteriID { get; set; }

            [StringLength(250)] public string? YuklemeAdres { get; set; }
            [StringLength(250)] public string? BosaltmaAdres { get; set; }

            [DataType(DataType.Date)] public DateTime? PlanlananYuklemeTarihi { get; set; }
            [DataType(DataType.Date)] public DateTime? YuklemeTarihi { get; set; }
            [DataType(DataType.Date)] public DateTime? GumrukCikisTarihi { get; set; }
            [DataType(DataType.Date)] public DateTime? VarisTarihi { get; set; }

            public IFormFile? CMRFile { get; set; }
            [StringLength(50)] public string? MRN { get; set; }

            // Durum inputtan gelmeyecek; her zaman 0 (Yeni) atanacak
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

                var today = DateTime.Today;
                Input.PlanlananYuklemeTarihi = today;
                Input.YuklemeTarihi = today;
                Input.GumrukCikisTarihi = today;
                Input.VarisTarihi = today;
            }

            await LoadSelectsAsync(Input.SiparisID, Input.DorseID, Input.YuklemeMusteriID, Input.BosaltmaMusteriID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            // Dorse zorunluluğunu bir kez daha kontrol et
            if (Input.DorseID == null)
                ModelState.AddModelError("Input.DorseID", "Dorse seçiniz.");

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SiparisID, Input.DorseID, Input.YuklemeMusteriID, Input.BosaltmaMusteriID);
                return Page();
            }

            // CMR dosyasını kaydet (varsa)
            string? cmrStoredName = null;
            if (Input.CMRFile is { Length: > 0 })
            {
                var okExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(Input.CMRFile.FileName).ToLowerInvariant();
                if (!okExt.Contains(ext))
                {
                    ModelState.AddModelError("Input.CMRFile", "Yalnızca PDF/JPG/PNG yükleyebilirsiniz.");
                    await LoadSelectsAsync(Input.SiparisID, Input.DorseID, Input.YuklemeMusteriID, Input.BosaltmaMusteriID);
                    return Page();
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "cmr");
                Directory.CreateDirectory(folder);
                cmrStoredName = $"CMR_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(folder, cmrStoredName);
                await using var fs = System.IO.File.Create(fullPath);
                await Input.CMRFile.CopyToAsync(fs);
            }

            var e = new Sevkiyat
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                SiparisID = Input.SiparisID,
                DorseID = Input.DorseID,

                YuklemeMusteriID = Input.YuklemeMusteriID,
                BosaltmaMusteriID = Input.BosaltmaMusteriID,
                YuklemeAdres = Input.YuklemeAdres?.Trim(),
                BosaltmaAdres = Input.BosaltmaAdres?.Trim(),

                PlanlananYuklemeTarihi = Input.PlanlananYuklemeTarihi?.Date,
                YuklemeTarihi = Input.YuklemeTarihi?.Date,
                GumrukCikisTarihi = Input.GumrukCikisTarihi?.Date,
                VarisTarihi = Input.VarisTarihi?.Date,

                CMRNo = cmrStoredName,
                MRN = Input.MRN?.Trim(),

                Durum = 0, // her zaman Yeni
                Notlar = Input.Notlar?.Trim()
            };

            _context.Sevkiyatlar.Add(e);
            await _context.SaveChangesAsync();

            // >>> YENİ: Sipariş durumunu 1 (Onaylı) yap
            var siparis = await _context.Siparisler
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SiparisID == e.SiparisID);

            if (siparis != null && siparis.Durum != 7 && siparis.Durum != 1)
            {
                siparis.Durum = 1;
                await _context.SaveChangesAsync();
            }
            // <<<

            // Kaydetten sonra İLGİLİ SİPARİŞİN detayına dön
            return RedirectToPage("/Siparisler/Details", new { id = e.SiparisID });
        }

        private async Task LoadSelectsAsync(int? siparisId, int? dorseId, int? yuklemeMusteriId, int? bosaltmaMusteriId)
        {
            var firmaId = User.GetFirmaId();

            // Sipariş listesi (sadece gösterim; UI'da disabled)
            SiparisSelect = new SelectList(
                await _context.Siparisler
                    .AsNoTracking()
                    .Where(x => x.FirmaID == firmaId)
                    .OrderByDescending(x => x.SiparisID)
                    .Select(x => new { x.SiparisID, Text = x.SiparisID + " - " + x.YukAciklamasi })
                    .ToListAsync(),
                "SiparisID", "Text", siparisId
            );

            // Dorse listesi (Araclar tablosunda IsDorse == true)
            DorselerSelect = new SelectList(
                await _context.Araclar
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && a.IsDorse)
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", dorseId
            );

            // Yükleme/Bosaltma müşteri select'lerini TEK seçenekle doldur (UI'da disabled)
            if (yuklemeMusteriId.HasValue)
            {
                var ad = await _context.Musteriler
                    .Where(m => m.MusteriID == yuklemeMusteriId.Value)
                    .Select(m => m.MusteriAdi)
                    .FirstOrDefaultAsync() ?? "—";

                YuklemeMusteriSelect = new SelectList(
                    new[] { new { MusteriID = yuklemeMusteriId.Value, Ad = ad } },
                    "MusteriID", "Ad", yuklemeMusteriId.Value);
            }
            else
            {
                YuklemeMusteriSelect = new SelectList(Enumerable.Empty<object>(), "X", "Y");
            }

            if (bosaltmaMusteriId.HasValue)
            {
                var ad = await _context.Musteriler
                    .Where(m => m.MusteriID == bosaltmaMusteriId.Value)
                    .Select(m => m.MusteriAdi)
                    .FirstOrDefaultAsync() ?? "—";

                BosaltmaMusteriSelect = new SelectList(
                    new[] { new { MusteriID = bosaltmaMusteriId.Value, Ad = ad } },
                    "MusteriID", "Ad", bosaltmaMusteriId.Value);
            }
            else
            {
                BosaltmaMusteriSelect = new SelectList(Enumerable.Empty<object>(), "X", "Y");
            }
        }
    }
}
