using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Lojistik.Pages.KademeFirmalari
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db) => _db = db;

        // KademeFirmalari artık Musteriler tablosunu kullanıyor (Kategori = 1 · Tedarikçi)
        // Yeni kayıt eklenirken UlkeID / SehirID için kullanılan varsayılan değerler:
        private const int VarsayilanUlkeID  = 1;
        private const int VarsayilanSehirID = 6;

        public List<Musteri> List { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }   // MusteriID

        [BindProperty]
        public NewVM NewItem { get; set; } = new();

        [BindProperty, ValidateNever]
        public EditVM EditItem { get; set; } = new();

        // ── View modelleri ───────────────────────────────────────────────────

        public class NewVM
        {
            [Required(ErrorMessage = "Ünvan zorunlu.")]
            [StringLength(200)]
            public string MusteriAdi { get; set; } = "";

            [StringLength(150)]
            public string? YetkiliIsim { get; set; }

            [StringLength(100)]
            public string? VergiDairesi { get; set; }

            [StringLength(50)]
            public string? VergiNo { get; set; }

            [StringLength(50)]
            public string? Telefon { get; set; }

            public bool IsActive { get; set; } = true;
        }

        public class EditVM
        {
            public int MusteriID { get; set; }

            [Required(ErrorMessage = "Ünvan zorunlu.")]
            [StringLength(200)]
            public string MusteriAdi { get; set; } = "";

            [StringLength(150)]
            public string? YetkiliIsim { get; set; }

            [StringLength(100)]
            public string? VergiDairesi { get; set; }

            [StringLength(50)]
            public string? VergiNo { get; set; }

            [StringLength(50)]
            public string? Telefon { get; set; }

            public bool IsActive { get; set; } = true;
        }

        // ── Yardımcı: listeyi yükle ─────────────────────────────────────────

        private async Task LoadAsync()
        {
            var firmaId = User.GetFirmaId();
            List = await _db.Musteriler
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId && m.Kategori == 1)   // 1 = Tedarikçi
                .OrderByDescending(m => m.IsActive)
                .ThenBy(m => m.MusteriAdi)
                .ToListAsync();
        }

        // ── GET ─────────────────────────────────────────────────────────────

        public async Task OnGetAsync()
        {
            await LoadAsync();

            if (EditId.HasValue)
            {
                var firmaId = User.GetFirmaId();
                var row = await _db.Musteriler
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MusteriID == EditId.Value
                                           && m.FirmaID   == firmaId
                                           && m.Kategori  == 1);
                if (row != null)
                {
                    EditItem = new EditVM
                    {
                        MusteriID    = row.MusteriID,
                        MusteriAdi   = row.MusteriAdi,
                        YetkiliIsim  = row.YetkiliIsim,
                        VergiDairesi = row.VergiDairesi,
                        VergiNo      = row.VergiNo,
                        Telefon      = row.Telefon,
                        IsActive     = row.IsActive
                    };
                }
                else
                {
                    EditId = null;
                }
            }
        }

        // ── EKLE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> OnPostAddAsync()
        {
            var firmaId = User.GetFirmaId();

            ModelState.Clear();
            TryValidateModel(NewItem, nameof(NewItem));
            if (!ModelState.IsValid) { await LoadAsync(); return Page(); }

            var adi = (NewItem.MusteriAdi ?? "").Trim();

            if (await _db.Musteriler.AnyAsync(m => m.FirmaID == firmaId && m.MusteriAdi == adi))
            {
                ModelState.AddModelError("NewItem.MusteriAdi", "Bu ünvan zaten kayıtlı.");
                await LoadAsync();
                return Page();
            }

            try
            {
                _db.Musteriler.Add(new Musteri
                {
                    FirmaID      = firmaId,
                    MusteriAdi   = adi,
                    YetkiliIsim  = NewItem.YetkiliIsim?.Trim(),
                    VergiDairesi = NewItem.VergiDairesi?.Trim(),
                    VergiNo      = NewItem.VergiNo?.Trim(),
                    Telefon      = NewItem.Telefon?.Trim(),
                    IsActive     = NewItem.IsActive,
                    Kategori     = 1,                     // Tedarikçi
                    UlkeID       = VarsayilanUlkeID,
                    SehirID      = VarsayilanSehirID,
                    CreatedAt    = DateTime.Now
                });
                await _db.SaveChangesAsync();
                TempData["StatusMessage"] = "Firma eklendi.";
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("UX_Musteriler_Firma_MusteriAdi"))
                    ModelState.AddModelError("NewItem.MusteriAdi", "Bu ünvan zaten kayıtlı.");
                else
                    ModelState.AddModelError("", "Kaydetme hatası: " + msg);
                await LoadAsync();
                return Page();
            }

            return RedirectToPage();
        }

        // ── DÜZENLE KAYDET ──────────────────────────────────────────────────

        public async Task<IActionResult> OnPostEditAsync()
        {
            var firmaId = User.GetFirmaId();

            ModelState.Clear();
            TryValidateModel(EditItem, nameof(EditItem));
            if (!ModelState.IsValid)
            {
                EditId = EditItem.MusteriID;
                await LoadAsync();
                return Page();
            }

            var row = await _db.Musteriler
                .FirstOrDefaultAsync(m => m.MusteriID == EditItem.MusteriID
                                       && m.FirmaID   == firmaId
                                       && m.Kategori  == 1);
            if (row == null) return NotFound();

            var adi = (EditItem.MusteriAdi ?? "").Trim();

            if (await _db.Musteriler.AnyAsync(m => m.FirmaID   == firmaId
                                                 && m.MusteriAdi == adi
                                                 && m.MusteriID  != row.MusteriID))
            {
                ModelState.AddModelError("EditItem.MusteriAdi", "Bu ünvan zaten kayıtlı.");
                EditId = row.MusteriID;
                await LoadAsync();
                return Page();
            }

            row.MusteriAdi   = adi;
            row.YetkiliIsim  = EditItem.YetkiliIsim?.Trim();
            row.VergiDairesi = EditItem.VergiDairesi?.Trim();
            row.VergiNo      = EditItem.VergiNo?.Trim();
            row.Telefon      = EditItem.Telefon?.Trim();
            row.IsActive     = EditItem.IsActive;

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Firma güncellendi.";
            return RedirectToPage();
        }

        // ── AKTİF / PASİF ───────────────────────────────────────────────────

        public async Task<IActionResult> OnPostToggleAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var row = await _db.Musteriler
                .FirstOrDefaultAsync(m => m.MusteriID == id
                                       && m.FirmaID   == firmaId
                                       && m.Kategori  == 1);
            if (row == null) return NotFound();

            row.IsActive = !row.IsActive;
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = row.IsActive ? "Firma aktif yapıldı." : "Firma pasif yapıldı.";
            return RedirectToPage();
        }

        // ── SİL ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var row = await _db.Musteriler
                .FirstOrDefaultAsync(m => m.MusteriID == id
                                       && m.FirmaID   == firmaId
                                       && m.Kategori  == 1);
            if (row == null) return NotFound();

            _db.Musteriler.Remove(row);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Firma silindi.";
            return RedirectToPage();
        }
    }
}
