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
            public int? DorseID { get; set; }

            public int? YuklemeMusteriID { get; set; }
            public int? BosaltmaMusteriID { get; set; }

            [StringLength(250)] public string? YuklemeAdres { get; set; }
            [StringLength(250)] public string? BosaltmaAdres { get; set; }

            [DataType(DataType.Date)] public DateTime? PlanlananYuklemeTarihi { get; set; }
            [DataType(DataType.Date)] public DateTime? YuklemeTarihi { get; set; }
            [DataType(DataType.Date)] public DateTime? GumrukCikisTarihi { get; set; }
            [DataType(DataType.Date)] public DateTime? VarisTarihi { get; set; }

            // CMR dosyası
            public IFormFile? CMRFile { get; set; }
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

                var today = DateTime.Today;
                Input.PlanlananYuklemeTarihi = today;
                Input.YuklemeTarihi = today;
                Input.GumrukCikisTarihi = today;
                Input.VarisTarihi = today;
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

            // CMR dosyasını kaydet (varsa)
            string? cmrStoredName = null;
            if (Input.CMRFile is { Length: > 0 })
            {
                var okExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(Input.CMRFile.FileName).ToLowerInvariant();
                if (!okExt.Contains(ext))
                {
                    ModelState.AddModelError("Input.CMRFile", "Yalnızca PDF/JPG/PNG yükleyebilirsiniz.");
                    await LoadSelectsAsync(Input.SiparisID, Input.DorseID, Input.YuklemeMusteriID);
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

                CMRNo = cmrStoredName, // dosya adı/path
                MRN = Input.MRN?.Trim(),

                Durum = Input.Durum,
                Notlar = Input.Notlar?.Trim()
            };

            _context.Sevkiyatlar.Add(e);
            await _context.SaveChangesAsync();

            // Sipariş durumu 1 (Onaylı) değilse 1’e çekmek istersen burada yapabilirsin:
            // var sip = await _context.Siparisler.FirstOrDefaultAsync(x => x.SiparisID == e.SiparisID && x.FirmaID == firmaId);
            // if (sip is not null && sip.Durum < 1) { sip.Durum = 1; await _context.SaveChangesAsync(); }

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
