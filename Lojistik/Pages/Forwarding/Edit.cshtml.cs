using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Forwarding
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public List<SelectListItem> YukuVerenSelect  { get; set; } = new();
        public List<SelectListItem> YukuAlanSelect   { get; set; } = new();
        public List<SelectListItem> TasiyiciSelect   { get; set; } = new();
        public List<Ulke> UlkeList { get; set; } = new();
        public string MusteriAdresJson { get; set; } = "{}";

        public class InputModel
        {
            public int ForwardingID { get; set; }
            public string? DosyaNo { get; set; }
            public int? YukuVerenID { get; set; }
            public int? YukuAlanID { get; set; }
            public int? TasiyiciMusteriID { get; set; }
            public string? TasiyiciAdi { get; set; }
            public string? TasiyiciUlke { get; set; }
            public string? YuklemeyeriAdi { get; set; }
            public string? VarisYeriAdi { get; set; }
            public DateTime? YuklemeTarihi { get; set; }
            public DateTime? TeslimTarihi { get; set; }
            public DateTime? GercekTeslimTarihi { get; set; }
            public string? EsyaCinsi { get; set; }
            public decimal? BrutKg { get; set; }
            public decimal? CbmHacim { get; set; }
            public string? CmrNo { get; set; }
            public string? TransitNo { get; set; }
            public string? GumrukBeyanNo { get; set; }
            public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var entity  = await _context.ForwardingIsler
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.ForwardingID == id && f.FirmaID == firmaId);

            if (entity == null) return NotFound();

            Input = new InputModel
            {
                ForwardingID       = entity.ForwardingID,
                DosyaNo            = entity.DosyaNo,
                YukuVerenID        = entity.YukuVerenID,
                YukuAlanID         = entity.YukuAlanID,
                TasiyiciMusteriID  = entity.TasiyiciMusteriID,
                TasiyiciAdi        = entity.TasiyiciAdi,
                TasiyiciUlke       = entity.TasiyiciUlke,
                YuklemeTarihi      = entity.YuklemeTarihi,
                TeslimTarihi       = entity.TeslimTarihi,
                GercekTeslimTarihi = entity.GercekTeslimTarihi,
                YuklemeyeriAdi     = entity.YuklemeyeriAdi,
                VarisYeriAdi       = entity.VarisYeriAdi,
                EsyaCinsi          = entity.EsyaCinsi,
                BrutKg             = entity.BrutKg,
                CbmHacim           = entity.CbmHacim,
                CmrNo              = entity.CmrNo,
                TransitNo          = entity.TransitNo,
                GumrukBeyanNo      = entity.GumrukBeyanNo,
                Notlar             = entity.Notlar
            };

            await DoldurAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            if (!ModelState.IsValid) { await DoldurAsync(); return Page(); }

            var entity = await _context.ForwardingIsler
                .FirstOrDefaultAsync(f => f.ForwardingID == Input.ForwardingID && f.FirmaID == firmaId);
            if (entity == null) return NotFound();

            var fkMusteriIds = new[] { Input.YukuVerenID, Input.YukuAlanID, Input.TasiyiciMusteriID }
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            if (fkMusteriIds.Count > 0)
            {
                var kontrolSayisi = await _context.Musteriler
                    .CountAsync(m => m.FirmaID == firmaId && fkMusteriIds.Contains(m.MusteriID));
                if (kontrolSayisi != fkMusteriIds.Count) return Forbid();
            }

            entity.DosyaNo            = Input.DosyaNo?.Trim();
            entity.YukuVerenID        = Input.YukuVerenID > 0 ? Input.YukuVerenID : null;
            entity.YukuAlanID         = Input.YukuAlanID > 0 ? Input.YukuAlanID : null;
            entity.TasiyiciMusteriID  = Input.TasiyiciMusteriID > 0 ? Input.TasiyiciMusteriID : null;
            entity.TasiyiciAdi        = Input.TasiyiciAdi?.Trim();
            entity.TasiyiciUlke       = Input.TasiyiciUlke?.Trim();
            entity.YuklemeTarihi      = Input.YuklemeTarihi?.Date;
            entity.TeslimTarihi       = Input.TeslimTarihi?.Date;
            entity.GercekTeslimTarihi = Input.GercekTeslimTarihi?.Date;
            entity.YuklemeyeriAdi     = Input.YuklemeyeriAdi?.Trim();
            entity.VarisYeriAdi       = Input.VarisYeriAdi?.Trim();
            entity.EsyaCinsi          = Input.EsyaCinsi?.Trim();
            entity.BrutKg             = Input.BrutKg;
            entity.CbmHacim           = Input.CbmHacim;
            entity.CmrNo              = Input.CmrNo?.Trim();
            entity.TransitNo          = Input.TransitNo?.Trim();
            entity.GumrukBeyanNo      = Input.GumrukBeyanNo?.Trim();
            entity.Notlar             = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Forwarding işi güncellendi.";
            return RedirectToPage("./Details", new { id = Input.ForwardingID });
        }

        // AJAX: Şehirler (modal için)
        public async Task<JsonResult> OnGetSehirlerAsync(int ulkeId)
        {
            var sehirler = await _context.Sehirler
                .Where(s => s.UlkeID == ulkeId && s.IsActive)
                .OrderBy(s => s.SehirAdi)
                .Select(s => new { s.SehirID, s.SehirAdi })
                .ToListAsync();
            return new JsonResult(sehirler);
        }

        // AJAX POST: Hızlı firma ekle
        public async Task<JsonResult> OnPostHizliMusteriAsync(
            [FromForm] string hizliAdi,
            [FromForm] byte hizliKategori,
            [FromForm] int hizliUlkeId,
            [FromForm] int hizliSehirId,
            [FromForm] string? hizliAdres)
        {
            var firmaId = User.GetFirmaId();
            if (string.IsNullOrWhiteSpace(hizliAdi) || hizliUlkeId <= 0 || hizliSehirId <= 0)
                return new JsonResult(new { success = false, error = "Ad, ülke ve şehir zorunludur." });

            var musteri = new Musteri
            {
                FirmaID    = firmaId,
                MusteriAdi = hizliAdi.Trim(),
                Kategori   = hizliKategori,
                UlkeID     = hizliUlkeId,
                SehirID    = hizliSehirId,
                Adres      = hizliAdres?.Trim(),
                IsActive   = true
            };
            _context.Musteriler.Add(musteri);
            try
            {
                await _context.SaveChangesAsync();
                var sehirAdi = (await _context.Sehirler.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SehirID == hizliSehirId))?.SehirAdi ?? "";
                var adresGorunum = string.Join(", ",
                    new[] { musteri.Adres, sehirAdi }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return new JsonResult(new
                {
                    success    = true,
                    musteriId  = musteri.MusteriID,
                    musteriAdi = musteri.MusteriAdi,
                    adres      = adresGorunum,
                    kategori   = (int)musteri.Kategori
                });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("UX_Musteriler_Firma_MusteriAdi"))
                    return new JsonResult(new { success = false, error = "Bu firma adı zaten kayıtlı." });
                return new JsonResult(new { success = false, error = "Kaydetme hatası." });
            }
        }

        private async Task DoldurAsync()
        {
            var firmaId = User.GetFirmaId();
            var musteriler = await _context.Musteriler
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId && m.IsActive)
                .Include(m => m.Sehir)
                .OrderBy(m => m.MusteriAdi)
                .ToListAsync();

            YukuVerenSelect = ToSelect(musteriler.Where(m => m.Kategori == 0));  // Sadece Müşteri
            YukuAlanSelect  = ToSelect(musteriler.Where(m => m.Kategori == 0));  // Sadece Müşteri
            TasiyiciSelect  = ToSelect(musteriler.Where(m => m.Kategori == 2));

            var adresMap = musteriler.ToDictionary(
                m => m.MusteriID.ToString(),
                m => string.Join(", ", new[] { m.Adres, m.Sehir?.SehirAdi }
                                            .Where(s => !string.IsNullOrWhiteSpace(s)))
            );
            MusteriAdresJson = JsonSerializer.Serialize(adresMap);

            UlkeList = await _context.Ulkeler
                .Where(u => u.IsActive)
                .OrderBy(u => u.UlkeAdi)
                .ToListAsync();
        }

        private static List<SelectListItem> ToSelect(IEnumerable<Musteri> list) =>
            list.Select(m => new SelectListItem { Value = m.MusteriID.ToString(), Text = m.MusteriAdi })
                .ToList();
    }
}
