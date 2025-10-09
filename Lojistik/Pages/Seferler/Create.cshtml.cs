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

namespace Lojistik.Pages.Seferler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? CekicilerSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }
        public SelectList? SoforlerSelect { get; set; } // ✅ yeni

        public class InputModel
        {
            [StringLength(30)] public string? SeferKodu { get; set; }
            [Required] public int AracID { get; set; }
            public int? DorseID { get; set; }

            // ✅ yeni: kayıtlı şoförden seçim
            public int? SoforID { get; set; }

            [StringLength(100)] public string? SurucuAdi { get; set; }

            public DateTime? CikisTarihi { get; set; }
            public DateTime? DonusTarihi { get; set; }

            [Required] public byte Durum { get; set; } = 0;
            [StringLength(500)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            // Sefer kodu
            Input.SeferKodu = await GenerateSeferKoduAsync();

            // Çıkış tarihi bugüne set
            Input.CikisTarihi = DateTime.Today;

            await LoadSelectsAsync(firmaId, null, null, null);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            // Elle veya listeden en az bir sürücü bilgisi zorunlu
            if (!Input.SoforID.HasValue && string.IsNullOrWhiteSpace(Input.SurucuAdi))
            {
                ModelState.AddModelError(string.Empty, "Lütfen kayıtlı bir şoför seçin veya sürücü adını elle girin.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(firmaId, Input.AracID, Input.DorseID, Input.SoforID);
                return Page();
            }

            // Sürücü adını belirle (öncelik elle girilende)
            string? surucuAdiFinal = Input.SurucuAdi?.Trim();
            if (string.IsNullOrWhiteSpace(surucuAdiFinal) && Input.SoforID.HasValue)
            {
                surucuAdiFinal = await _context.Soforler
                    .Where(x => x.FirmaID == firmaId && x.SoforID == Input.SoforID.Value)
                    .Select(x => x.AdSoyad)
                    .FirstOrDefaultAsync();
            }

            var e = new Sefer
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,
                SeferKodu = Input.SeferKodu ?? await GenerateSeferKoduAsync(),
                AracID = Input.AracID,
                DorseID = Input.DorseID,
                SurucuAdi = surucuAdiFinal,
                CikisTarihi = Input.CikisTarihi ?? DateTime.Today,
                DonusTarihi = Input.DonusTarihi?.Date,
                Durum = Input.Durum,
                Notlar = Input.Notlar?.Trim()
            };

            _context.Seferler.Add(e);
            await _context.SaveChangesAsync();

            // Dorse bağlı bekleyen sevkiyat varsa Sefer’e bağla ve sipariş durumunu güncelle
            if (Input.DorseID.HasValue)
            {
                var sevkiyat = await _context.Sevkiyatlar
                    .Include(s => s.Siparis)
                    .FirstOrDefaultAsync(s =>
                        s.FirmaID == firmaId &&
                        s.DorseID == Input.DorseID &&
                        s.Siparis.Durum == 1);

                if (sevkiyat != null)
                {
                    var baglanti = new SeferSevkiyat
                    {
                        FirmaID = firmaId,
                        SeferID = e.SeferID,
                        SevkiyatID = sevkiyat.SevkiyatID
                    };

                    _context.SeferSevkiyatlar.Add(baglanti);

                    // Siparişi 2: Hazırlanıyor olsun
                    sevkiyat.Siparis.Durum = 2;

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage("./Details", new { id = e.SeferID });
        }

        private async Task LoadSelectsAsync(int firmaId, int? cekiciId, int? dorseId, int? soforId)
        {
            // Çekici
            CekicilerSelect = new SelectList(
                await _context.Araclar.AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && (a.IsDorse == false || a.IsDorse == null))
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", cekiciId
            );

            // Dorse (Siparis.Durum == 1 olan sevkiyatlara bağlı dorseler)
            DorselerSelect = new SelectList(
                await _context.Sevkiyatlar.AsNoTracking()
                    .Where(s => s.FirmaID == firmaId && s.DorseID != null && s.Siparis.Durum == 1)
                    .Select(s => new
                    {
                        s.DorseID,
                        Text = s.Dorse!.Plaka + " - " +
                               (s.Siparis.AliciMusteri != null && s.Siparis.AliciMusteri.Ulke != null
                                   ? s.Siparis.AliciMusteri.Ulke.UlkeAdi
                                   : "Ülke Yok") +
                               " - " +
                               (s.Siparis.AliciMusteri != null ? s.Siparis.AliciMusteri.MusteriAdi : "Müşteri Yok")
                    })
                    .Distinct()
                    .ToListAsync(),
                "DorseID", "Text", dorseId
            );

            // ✅ Şoför (yalnızca Aktif olanlar)
            SoforlerSelect = new SelectList(
                await _context.Soforler.AsNoTracking()
                    .Where(s => s.FirmaID == firmaId && s.Durum == 1)
                    .OrderBy(s => s.AdSoyad)
                    .Select(s => new { s.SoforID, s.AdSoyad })
                    .ToListAsync(),
                "SoforID", "AdSoyad", soforId
            );
        }

        private async Task<string> GenerateSeferKoduAsync()
        {
            var today = DateTime.Now;
            var kodTarih = today.ToString("MMyy"); // örn: 1025

            var countToday = await _context.Seferler.CountAsync(s => s.CreatedAt.Date == today.Date);

            return $"{kodTarih}-{countToday + 1:D3}";
        }
    }
}
