using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Forwarding
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public ForwardingIs Is { get; set; } = null!;
        public List<ForwardingFiyat> Fiyatlar { get; set; } = new();
        public List<ForwardingKalem> Kalemler { get; set; } = new();

        // Kâr/zarar özeti per PB
        public Dictionary<string, decimal> SatisToplam { get; set; } = new();
        public Dictionary<string, decimal> AlisToplam  { get; set; } = new();

        [BindProperty] public FiyatInput  YeniFiyat  { get; set; } = new();
        [BindProperty] public KalemInput  YeniKalem  { get; set; } = new();

        public class FiyatInput
        {
            public string FiyatTuru  { get; set; } = "Satis";
            public string? Kalem     { get; set; }
            public decimal Tutar     { get; set; }
            public string ParaBirimi { get; set; } = "EUR";
            public string? Aciklama  { get; set; }
        }

        public class KalemInput
        {
            public decimal Adet     { get; set; }
            public string  Nevi     { get; set; } = "Palet";
            public string? Aciklama { get; set; }
        }

        // ─── GET ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!await YukleAsync(id)) return NotFound();
            return Page();
        }

        // ─── Kalem ekle ──────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostKalemEkleAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            if (!await YukleAsync(id)) return NotFound();

            if (YeniKalem.Adet <= 0)
            {
                ModelState.AddModelError("YeniKalem.Adet", "Adet sıfırdan büyük olmalıdır.");
                return Page();
            }
            if (string.IsNullOrWhiteSpace(YeniKalem.Nevi))
            {
                ModelState.AddModelError("YeniKalem.Nevi", "Nevi / birim gereklidir.");
                return Page();
            }

            _context.ForwardingKalemler.Add(new ForwardingKalem
            {
                FirmaID      = firmaId,
                ForwardingID = id,
                Adet         = YeniKalem.Adet,
                Nevi         = YeniKalem.Nevi.Trim(),
                Aciklama     = YeniKalem.Aciklama?.Trim(),
                CreatedAt    = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Kalem eklendi.";
            return RedirectToPage(new { id });
        }

        // ─── Kalem sil ───────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostKalemSilAsync(int id, int kalemId)
        {
            var firmaId = User.GetFirmaId();
            var kalem = await _context.ForwardingKalemler
                .FirstOrDefaultAsync(k => k.KalemID == kalemId && k.FirmaID == firmaId);

            if (kalem != null)
            {
                _context.ForwardingKalemler.Remove(kalem);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Kalem silindi.";
            }
            return RedirectToPage(new { id });
        }

        // ─── Fiyat ekle ──────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostFiyatEkleAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            if (!await YukleAsync(id)) return NotFound();

            if (YeniFiyat.Tutar <= 0)
            {
                ModelState.AddModelError("YeniFiyat.Tutar", "Tutar sıfırdan büyük olmalıdır.");
                return Page();
            }

            _context.ForwardingFiyatlar.Add(new ForwardingFiyat
            {
                FirmaID      = firmaId,
                ForwardingID = id,
                FiyatTuru    = YeniFiyat.FiyatTuru == "Alis" ? "Alis" : "Satis",
                Kalem        = YeniFiyat.Kalem?.Trim(),
                Tutar        = YeniFiyat.Tutar,
                ParaBirimi   = (YeniFiyat.ParaBirimi ?? "EUR").Trim().ToUpperInvariant(),
                Aciklama     = YeniFiyat.Aciklama?.Trim(),
                CreatedAt    = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Fiyat eklendi.";
            return RedirectToPage(new { id });
        }

        // ─── Fiyat sil ───────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostFiyatSilAsync(int id, int fiyatId)
        {
            var firmaId = User.GetFirmaId();
            var fiyat = await _context.ForwardingFiyatlar
                .FirstOrDefaultAsync(f => f.FiyatID == fiyatId && f.FirmaID == firmaId);

            if (fiyat != null)
            {
                _context.ForwardingFiyatlar.Remove(fiyat);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Fiyat silindi.";
            }
            return RedirectToPage(new { id });
        }

        // ─── Durum güncelle ──────────────────────────────────────────────────
        public async Task<IActionResult> OnPostDurumGuncelleAsync(int id, byte yeniDurum)
        {
            var firmaId = User.GetFirmaId();
            var entity = await _context.ForwardingIsler
                .FirstOrDefaultAsync(f => f.ForwardingID == id && f.FirmaID == firmaId);

            if (entity == null) return NotFound();

            entity.Durum = yeniDurum;
            if (yeniDurum == 3 && entity.GercekTeslimTarihi == null)
                entity.GercekTeslimTarihi = DateTime.Today;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"Durum: {ForwardingIs.DurumAdi(yeniDurum)}";
            return RedirectToPage(new { id });
        }

        // ─── YukleAsync ──────────────────────────────────────────────────────
        private async Task<bool> YukleAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            Is = await _context.ForwardingIsler
                .Include(f => f.Musteri)
                .Include(f => f.YukuVeren).ThenInclude(m => m!.Sehir)
                .Include(f => f.YukuAlan).ThenInclude(m => m!.Sehir)
                .Include(f => f.TasiyiciMusteri).ThenInclude(m => m!.Sehir)
                .FirstOrDefaultAsync(f => f.ForwardingID == id && f.FirmaID == firmaId);

            if (Is == null) return false;

            Kalemler = await _context.ForwardingKalemler
                .AsNoTracking()
                .Where(k => k.ForwardingID == id && k.FirmaID == firmaId)
                .OrderBy(k => k.KalemID)
                .ToListAsync();

            Fiyatlar = await _context.ForwardingFiyatlar
                .AsNoTracking()
                .Where(f => f.ForwardingID == id && f.FirmaID == firmaId)
                .OrderBy(f => f.FiyatTuru).ThenBy(f => f.FiyatID)
                .ToListAsync();

            SatisToplam = Fiyatlar.Where(f => f.FiyatTuru == "Satis")
                .GroupBy(f => f.ParaBirimi)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Tutar));

            AlisToplam = Fiyatlar.Where(f => f.FiyatTuru == "Alis")
                .GroupBy(f => f.ParaBirimi)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Tutar));

            return true;
        }
    }
}
