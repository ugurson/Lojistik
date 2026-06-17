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
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        // Kategoriye göre ayrı listeler
        public List<SelectListItem> YukuVerenSelect   { get; set; } = new(); // Müşteri + Tedarikçi
        public List<SelectListItem> YukuAlanSelect    { get; set; } = new(); // Müşteri + Tedarikçi
        public List<SelectListItem> TasiyiciSelect    { get; set; } = new(); // Nakliyeci
        public List<SelectListItem> TumMusteriSelect  { get; set; } = new(); // Tümü (modal için)
        public List<Ulke> UlkeList { get; set; } = new();
        public string MusteriAdresJson { get; set; } = "{}";

        public class InputModel
        {
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
            public string? EsyaCinsi { get; set; }
            public decimal? BrutKg { get; set; }
            public decimal? CbmHacim { get; set; }
            public string? CmrNo { get; set; }
            public string? TransitNo { get; set; }
            public string? GumrukBeyanNo { get; set; }
            public string? Notlar { get; set; }

            /// <summary>Oluşturma sırasında eklenen ambalaj kalemleri</summary>
            public List<KalemSatir> Kalemler { get; set; } = new();
        }

        public class KalemSatir
        {
            public decimal Adet { get; set; }
            public string  Nevi { get; set; } = "Palet";
            public string? Aciklama { get; set; }
        }

        public async Task OnGetAsync() => await DoldurAsync();

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId  = User.GetUserId();

            if (!ModelState.IsValid) { await DoldurAsync(); return Page(); }

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

            var entity = new ForwardingIs
            {
                FirmaID              = firmaId,
                DosyaNo              = Input.DosyaNo?.Trim(),
                YukuVerenID          = Input.YukuVerenID > 0 ? Input.YukuVerenID : null,
                YukuAlanID           = Input.YukuAlanID > 0 ? Input.YukuAlanID : null,
                TasiyiciMusteriID    = Input.TasiyiciMusteriID > 0 ? Input.TasiyiciMusteriID : null,
                TasiyiciAdi          = Input.TasiyiciAdi?.Trim(),
                TasiyiciUlke         = Input.TasiyiciUlke?.Trim(),
                YuklemeTarihi        = Input.YuklemeTarihi?.Date,
                TeslimTarihi         = Input.TeslimTarihi?.Date,
                YuklemeyeriAdi       = Input.YuklemeyeriAdi?.Trim(),
                VarisYeriAdi         = Input.VarisYeriAdi?.Trim(),
                EsyaCinsi            = Input.EsyaCinsi?.Trim(),
                BrutKg               = Input.BrutKg,
                CbmHacim             = Input.CbmHacim,
                CmrNo                = Input.CmrNo?.Trim(),
                TransitNo            = Input.TransitNo?.Trim(),
                GumrukBeyanNo        = Input.GumrukBeyanNo?.Trim(),
                Notlar               = Input.Notlar?.Trim(),
                Durum                = 0,
                CreatedByKullaniciID = userId,
                CreatedAt            = DateTime.Now
            };

            _context.ForwardingIsler.Add(entity);
            await _context.SaveChangesAsync();

            // Kalem satırlarını kaydet
            var gecerliKalemler = Input.Kalemler
                .Where(k => k.Adet > 0 && !string.IsNullOrWhiteSpace(k.Nevi))
                .ToList();
            foreach (var k in gecerliKalemler)
            {
                _context.ForwardingKalemler.Add(new ForwardingKalem
                {
                    FirmaID      = firmaId,
                    ForwardingID = entity.ForwardingID,
                    Adet         = k.Adet,
                    Nevi         = k.Nevi.Trim(),
                    Aciklama     = k.Aciklama?.Trim(),
                    CreatedAt    = DateTime.Now
                });
            }
            if (gecerliKalemler.Any())
                await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Forwarding işi oluşturuldu.";
            return RedirectToPage("./Details", new { id = entity.ForwardingID });
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

            // Kategori bazlı filtreler
            // 0=Müşteri, 1=Tedarikçi, 2=Nakliyeci
            YukuVerenSelect  = ToSelect(musteriler.Where(m => m.Kategori == 0));  // Sadece Müşteri
            YukuAlanSelect   = ToSelect(musteriler.Where(m => m.Kategori == 0));  // Sadece Müşteri
            TasiyiciSelect   = ToSelect(musteriler.Where(m => m.Kategori == 2));
            TumMusteriSelect = ToSelect(musteriler);

            // Adres sözlüğü — JS otomatik doldurma için
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
