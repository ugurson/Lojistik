using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
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
            [Required] public int SevkiyatID { get; set; }
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
            public string? ExistingCMR { get; set; }

            [StringLength(50)] public string? MRN { get; set; }
            [StringLength(500)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var e = await _context.Sevkiyatlar
                .Include(x => x.Siparis).ThenInclude(s => s.GonderenMusteri)
                .Include(x => x.Siparis).ThenInclude(s => s.AliciMusteri)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == id);

            if (e == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SevkiyatID = e.SevkiyatID,
                SiparisID = e.SiparisID,
                DorseID = e.DorseID,
                YuklemeMusteriID = e.YuklemeMusteriID ?? 0,
                BosaltmaMusteriID = e.BosaltmaMusteriID ?? 0,
                YuklemeAdres = e.YuklemeAdres,
                BosaltmaAdres = e.BosaltmaAdres,
                PlanlananYuklemeTarihi = e.PlanlananYuklemeTarihi,
                YuklemeTarihi = e.YuklemeTarihi,
                GumrukCikisTarihi = e.GumrukCikisTarihi,
                VarisTarihi = e.VarisTarihi,
                ExistingCMR = e.CMRNo,
                MRN = e.MRN,
                Notlar = e.Notlar
            };

            await LoadSelectsAsync(firmaId, Input.DorseID, Input.YuklemeMusteriID, Input.BosaltmaMusteriID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input is null) return RedirectToPage("./Index");

            var firmaId = User.GetFirmaId();

            // Dorse boş ise uyarı verip beklet
            if (Input.DorseID == null)
                ModelState.AddModelError("Input.DorseID", "Dorse seçiniz.");

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(firmaId, Input.DorseID, Input.YuklemeMusteriID, Input.BosaltmaMusteriID);
                return Page();
            }

            var e = await _context.Sevkiyatlar
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == Input.SevkiyatID);
            if (e == null) return RedirectToPage("./Index");

            // CMR dosyası
            if (Input.CMRFile is { Length: > 0 })
            {
                var okExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(Input.CMRFile.FileName).ToLowerInvariant();
                if (!okExt.Contains(ext))
                {
                    ModelState.AddModelError("Input.CMRFile", "Yalnızca PDF/JPG/PNG yükleyebilirsiniz.");
                    await LoadSelectsAsync(firmaId, Input.DorseID, Input.YuklemeMusteriID, Input.BosaltmaMusteriID);
                    return Page();
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "cmr");
                Directory.CreateDirectory(folder);
                var newName = $"CMR_{Guid.NewGuid():N}{ext}";
                var full = Path.Combine(folder, newName);
                await using (var fs = System.IO.File.Create(full))
                    await Input.CMRFile.CopyToAsync(fs);

                if (!string.IsNullOrWhiteSpace(e.CMRNo))
                {
                    var old = Path.Combine(folder, e.CMRNo);
                    if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
                }
                e.CMRNo = newName;
            }

            // Alanları güncelle (Durum'a dokunmuyoruz)
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
            e.Notlar = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();

            // Kayıt sonrası → Sipariş Detayı
            return RedirectToPage("/Siparisler/Details", new { id = e.SiparisID });
        }

        private async Task LoadSelectsAsync(int firmaId, int? dorseId, int? yuklemeMusteriId, int? bosaltmaMusteriId)
        {
            DorselerSelect = new SelectList(
                await _context.Araclar
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && a.IsDorse)
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", dorseId
            );

            if (yuklemeMusteriId.HasValue)
            {
                var ad = await _context.Musteriler
                    .AsNoTracking()
                    .Where(m => m.MusteriID == yuklemeMusteriId.Value && m.FirmaID == firmaId)
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
                    .AsNoTracking()
                    .Where(m => m.MusteriID == bosaltmaMusteriId.Value && m.FirmaID == firmaId)
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
