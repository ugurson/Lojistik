// Pages/Siparisler/Create.cshtml.cs
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lojistik.Pages.Siparisler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? MusterilerSelect { get; set; }
        public SelectList? AraTedarikciSelect { get; set; }
        public SelectList? ParaBirimleriSelect { get; set; }

        public class InputModel
        {
            [DataType(DataType.Date)] public DateTime SiparisTarihi { get; set; } = DateTime.Today;

            [Required] public int GonderenMusteriID { get; set; }
            [Required] public int AliciMusteriID { get; set; }
            public int? AraTedarikciMusteriID { get; set; }

            public int? Adet { get; set; }
            [StringLength(50)] public string? AdetCinsi { get; set; }
            public int? Kilo { get; set; }

            [Required, StringLength(200)] public string YukAciklamasi { get; set; } = null!;
            [Column(TypeName = "decimal(18,2)")]
            public decimal? Tutar { get; set; }
            [StringLength(10)] public string? ParaBirimi { get; set; } = "TRY";

            [StringLength(50)] public string? FaturaNo { get; set; }
            [StringLength(500)] public string? Notlar { get; set; }

            [Required] public byte Durum { get; set; } = 0;

            // YENİ: 1 = Yurtdışı (default), 2 = Yurtiçi
            public int SiparisTur { get; set; } = 1;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync();
                return Page();
            }

            // GonderenMusteriID ve AliciMusteriID zorunlu FK — firma kontrolü
            var zorunluIds = new[] { Input.GonderenMusteriID, Input.AliciMusteriID }
                .Distinct().ToList();
            var zorunluSayisi = await _context.Musteriler
                .CountAsync(m => m.FirmaID == firmaId && zorunluIds.Contains(m.MusteriID));
            if (zorunluSayisi != zorunluIds.Count) return Forbid();

            // AraTedarikciMusteriID opsiyonel
            if (Input.AraTedarikciMusteriID is > 0)
            {
                var araAit = await _context.Musteriler
                    .AnyAsync(m => m.FirmaID == firmaId && m.MusteriID == Input.AraTedarikciMusteriID.Value);
                if (!araAit) return Forbid();
            }

            var e = new Siparis
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                SiparisTarihi = Input.SiparisTarihi.Date,
                GonderenMusteriID = Input.GonderenMusteriID,
                AliciMusteriID = Input.AliciMusteriID,
                AraTedarikciMusteriID = Input.AraTedarikciMusteriID,

                Adet = Input.Adet,
                AdetCinsi = Input.AdetCinsi?.Trim(),
                Kilo = Input.Kilo,

                YukAciklamasi = Input.YukAciklamasi.Trim(),
                Tutar = Input.Tutar,
                ParaBirimi = string.IsNullOrWhiteSpace(Input.ParaBirimi) ? null : Input.ParaBirimi.Trim(),

                FaturaNo = Input.FaturaNo?.Trim(),
                Notlar = Input.Notlar?.Trim(),
                Durum = Input.Durum,

                // YENİ: Sipariş Türü
                SiparisTur = Input.SiparisTur
            };

            _context.Siparisler.Add(e);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = e.SiparisID });
        }

        // AJAX: ?handler=Ulkeler
        public async Task<JsonResult> OnGetUlkelerAsync()
        {
            var list = await _context.Ulkeler
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.UlkeAdi)
                .Select(u => new { u.UlkeID, u.UlkeAdi })
                .ToListAsync();
            return new JsonResult(list);
        }

        // AJAX: ?handler=Sehirler&ulkeId=#
        public async Task<JsonResult> OnGetSehirlerAsync(int ulkeId)
        {
            var list = await _context.Sehirler
                .AsNoTracking()
                .Where(s => s.UlkeID == ulkeId && s.IsActive)
                .OrderBy(s => s.SehirAdi)
                .Select(s => new { s.SehirID, s.SehirAdi })
                .ToListAsync();
            return new JsonResult(list);
        }

        // AJAX POST: ?handler=QuickMusteri
        public async Task<JsonResult> OnPostQuickMusteriAsync(
            [FromBody] QuickMusteriInput input)
        {
            if (string.IsNullOrWhiteSpace(input?.MusteriAdi))
                return new JsonResult(new { ok = false, hata = "Müşteri adı zorunludur." });

            var firmaId = User.GetFirmaId();

            var mevcut = await _context.Musteriler
                .AsNoTracking()
                .AnyAsync(m => m.FirmaID == firmaId && m.MusteriAdi == input.MusteriAdi.Trim());
            if (mevcut)
                return new JsonResult(new { ok = false, hata = "Bu isimde bir müşteri zaten kayıtlı." });

            if (input.UlkeID <= 0)
                return new JsonResult(new { ok = false, hata = "Ülke seçimi zorunludur." });
            if (input.SehirID <= 0)
                return new JsonResult(new { ok = false, hata = "Şehir seçimi zorunludur." });

            var musteri = new Musteri
            {
                FirmaID  = firmaId,
                MusteriAdi = input.MusteriAdi.Trim(),
                Kategori = input.Kategori,
                UlkeID   = input.UlkeID,
                SehirID  = input.SehirID,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Musteriler.Add(musteri);
            await _context.SaveChangesAsync();

            return new JsonResult(new { ok = true, id = musteri.MusteriID, ad = musteri.MusteriAdi });
        }

        public class QuickMusteriInput
        {
            public string? MusteriAdi { get; set; }
            public byte    Kategori   { get; set; } = 0;
            public int     UlkeID     { get; set; }
            public int     SehirID    { get; set; }
        }

        private async Task LoadSelectsAsync()
        {
            var firmaId = User.GetFirmaId();

            var musteriList = await _context.Musteriler
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId)
                .OrderBy(m => m.MusteriAdi)
                .Select(m => new { m.MusteriID, m.MusteriAdi })
                .ToListAsync();

            MusterilerSelect = new SelectList(musteriList, "MusteriID", "MusteriAdi");
            AraTedarikciSelect = new SelectList(musteriList, "MusteriID", "MusteriAdi");

            var pbs = new[] { "TRY", "USD", "EUR", "GBP", "CHF" };
            ParaBirimleriSelect = new SelectList(pbs.Select(x => new { Value = x, Text = x }), "Value", "Text", Input.ParaBirimi);
        }
    }
}
