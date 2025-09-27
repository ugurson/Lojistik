using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Sevkiyatlar
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public EditModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty] public InputModel? Input { get; set; }

        public SelectList? DorselerSelect { get; set; }
        public SelectList? YuklemeMusteriSelect { get; set; }
        public SelectList? BosaltmaMusteriSelect { get; set; }

        public class InputModel
        {
            public int SevkiyatID { get; set; }
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

            public IFormFile? CMRFile { get; set; }
            public string? ExistingCMR { get; set; }

            [StringLength(50)] public string? MRN { get; set; }
            [Required] public byte Durum { get; set; } = 0;
            [StringLength(500)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.Sevkiyatlar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == id);

            if (e == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SevkiyatID = e.SevkiyatID,
                SiparisID = e.SiparisID,
                DorseID = e.DorseID,
                YuklemeMusteriID = e.YuklemeMusteriID,
                BosaltmaMusteriID = e.BosaltmaMusteriID,
                YuklemeAdres = e.YuklemeAdres,
                BosaltmaAdres = e.BosaltmaAdres,
                PlanlananYuklemeTarihi = e.PlanlananYuklemeTarihi,
                YuklemeTarihi = e.YuklemeTarihi,
                GumrukCikisTarihi = e.GumrukCikisTarihi,
                VarisTarihi = e.VarisTarihi,
                ExistingCMR = e.CMRNo,
                MRN = e.MRN,
                Durum = e.Durum,
                Notlar = e.Notlar
            };

            await LoadSelectsAsync(firmaId, Input.DorseID, Input.YuklemeMusteriID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input is null) return RedirectToPage("./Index");
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(firmaId, Input.DorseID, Input.YuklemeMusteriID);
                return Page();
            }

            var e = await _context.Sevkiyatlar
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == Input.SevkiyatID);

            if (e == null) return RedirectToPage("./Index");

            // CMR yükleme (yeni dosya seçildiyse değiştir)
            if (Input.CMRFile is { Length: > 0 })
            {
                var okExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(Input.CMRFile.FileName).ToLowerInvariant();
                if (!okExt.Contains(ext))
                {
                    ModelState.AddModelError("Input.CMRFile", "Yalnızca PDF/JPG/PNG yükleyebilirsiniz.");
                    await LoadSelectsAsync(firmaId, Input.DorseID, Input.YuklemeMusteriID);
                    return Page();
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "cmr");
                Directory.CreateDirectory(folder);
                var newName = $"CMR_{Guid.NewGuid():N}{ext}";
                var full = Path.Combine(folder, newName);
                await using (var fs = System.IO.File.Create(full))
                    await Input.CMRFile.CopyToAsync(fs);

                // eski dosyayı sil
                if (!string.IsNullOrWhiteSpace(e.CMRNo))
                {
                    var oldPath = Path.Combine(folder, e.CMRNo);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                e.CMRNo = newName;
            }

            // alanları güncelle
            e.DorseID = Input.DorseID;
            e.YuklemeMusteriID = Input.YuklemeMusteriID;
            e.BosaltmaMusteriID = Input.BosaltmaMusteriID;
            e.YuklemeAdres = Input.YuklemeAdres?.Trim();
            e.BosaltmaAdres = Input.BosaltmaAdres?.Trim();
            e.PlanlananYuklemeTarihi = Input.PlanlananYuklemeTarihi?.Date;
            e.YuklemeTarihi = Input.YuklemeTarihi?.Date;
            e.GumrukCikisTarihi = Input.GumrukCikisTarihi?.Date;
            e.VarisTarihi = Input.VarisTarihi?.Date;
            e.MRN = Input.MRN?.Trim();
            e.Durum = Input.Durum;
            e.Notlar = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = e.SevkiyatID });
        }

        private async Task LoadSelectsAsync(int firmaId, int? dorseId, int? yuklemeMusteriId)
        {
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
