using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
// Details.cshtml.cs en üstüne
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Lojistik.Pages.Seferler
{
    // İstersen tüm POST'lar için sınıf seviyesinde aç:
    // [ValidateAntiForgeryToken]
    public class DetailsModel : PageModel
    {
        public bool IsClosed { get; private set; }

        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public SeferItem? Data { get; set; }
        public List<SiparisRow> Siparisler { get; set; } = new();
        public List<SelectListItem> MusteriOptions { get; set; } = new();

        public record SeferItem(
            int SeferID,
            string? SeferKodu,
            string? AracPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi,
            byte Durum,
            string? Notlar,
            DateTime CreatedAt
        );

        public record MasrafRow(
            int SeferMasrafID,
            DateTime Tarih,
            string MasrafTipi,
            decimal Tutar,
            string ParaBirimi,
            string? FaturaBelgeNo,
            string? Ulke,
            string? Yer,
            string? Notlar
        );

        public record GelirRow(
            int SeferGelirID,
            DateTime Tarih,
            decimal Tutar,
            string ParaBirimi,
            string? Aciklama,
            int? IlgiliSiparisID,
            string? Notlar,
    string? FaturaNo,
    string? CikisIl,
    string? VarisIl,
    bool IsCarilestirildi,
    string? CariMusteriAdi,
    string? CariIslemTuru
        );

        public List<GelirRow> Gelirler { get; set; } = new();
        public List<MasrafRow> Masraflar { get; set; } = new();
        public Dictionary<string, decimal> MasrafToplamlari { get; set; } = new();
        public Dictionary<string, decimal> GelirToplamlari { get; set; } = new();

        public record SiparisRow(
            int SiparisID,
            DateTime SiparisTarihi,
            string YukAciklamasi,
            string? Gonderen,
            string? Alici,
            string? AliciUlke,
            string? AliciSehir,
            decimal? Tutar,
            string? ParaBirimi,
            string? FaturaNo,
            string? DorsePlaka
        );

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();



            Data = await _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SeferID == id)
                .Select(s => new SeferItem(
                    s.SeferID,
                    s.SeferKodu,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.CikisTarihi,
                    s.DonusTarihi,
                    s.Durum,
                    s.Notlar,
                    s.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");

            MusteriOptions = await _context.Musteriler
.AsNoTracking()
.Where(m => m.FirmaID == firmaId)
.OrderBy(m => m.MusteriAdi)
.Select(m => new SelectListItem
{
Value = m.MusteriID.ToString(),
Text = m.MusteriAdi
})
.ToListAsync();

            Siparisler = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .Where(x => x.Sefer.FirmaID == firmaId && x.SeferID == id)
                .Select(x => new SiparisRow(
                    x.Sevkiyat.Siparis.SiparisID,
                    x.Sevkiyat.Siparis.SiparisTarihi,
                    x.Sevkiyat.Siparis.YukAciklamasi,
                    x.Sevkiyat.Siparis.GonderenMusteri != null ? x.Sevkiyat.Siparis.GonderenMusteri.MusteriAdi : null,
                    x.Sevkiyat.Siparis.AliciMusteri != null ? x.Sevkiyat.Siparis.AliciMusteri.MusteriAdi : null,
                    x.Sevkiyat.Siparis.AliciMusteri != null && x.Sevkiyat.Siparis.AliciMusteri.Ulke != null
                        ? x.Sevkiyat.Siparis.AliciMusteri.Ulke.UlkeAdi : null,
                    x.Sevkiyat.Siparis.AliciMusteri != null && x.Sevkiyat.Siparis.AliciMusteri.Sehir != null
                        ? x.Sevkiyat.Siparis.AliciMusteri.Sehir.SehirAdi : null,
                    x.Sevkiyat.Siparis.Tutar,
                    x.Sevkiyat.Siparis.ParaBirimi,
                    x.Sevkiyat.Siparis.FaturaNo,
        x.Sevkiyat.Dorse != null ? x.Sevkiyat.Dorse.Plaka : null   // <-- EKLENDİ

                ))
                .ToListAsync();

            Masraflar = await _context.SeferMasraflari
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId && m.SeferID == id)
                .OrderByDescending(m => m.Tarih)
                .Select(m => new MasrafRow(
                    m.SeferMasrafID,
                    m.Tarih,
                    m.MasrafTipi,
                    m.Tutar,
                    m.ParaBirimi,
                    m.FaturaBelgeNo,
                    m.Ulke,
                    m.Yer,
                    m.Notlar
                ))
                .ToListAsync();

            MasrafToplamlari = await _context.SeferMasraflari
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId && m.SeferID == id)
                .GroupBy(m => m.ParaBirimi)
                .Select(g => new { PB = g.Key!, Sum = g.Sum(x => x.Tutar) })
                .ToDictionaryAsync(x => x.PB, x => x.Sum);

            Gelirler = await _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.FirmaID == firmaId && g.SeferID == id)
                .OrderByDescending(g => g.Tarih)
                .Select(g => new GelirRow(
        g.SeferGelirID,
        g.Tarih,
        g.Tutar,
        g.ParaBirimi,
        g.Aciklama,
        g.IlgiliSiparisID,
        g.Notlar,
        _context.Siparisler
            .Where(s => s.FirmaID == firmaId && s.SiparisID == g.IlgiliSiparisID)
            .Select(s => s.FaturaNo)
            .FirstOrDefault(),
        g.CikisIl,
        g.VarisIl,
false,  // IsCarilestirildi
        null,   // CariMusteriAdi
        null 
    ))
    .ToListAsync();
            var gelirIds = Gelirler.Select(x => x.SeferGelirID).ToList();

            var cariList = await _context.CariHareketler
    .AsNoTracking()
    .Where(ch => ch.FirmaID == firmaId && ch.SeferGelirID != null && gelirIds.Contains(ch.SeferGelirID.Value))
    .Join(
        _context.Musteriler.AsNoTracking().Where(m => m.FirmaID == firmaId),
        ch => new { ch.FirmaID, ch.MusteriID },
        m => new { m.FirmaID, m.MusteriID },
        (ch, m) => new
        {
            SeferGelirID = ch.SeferGelirID!.Value,
            MusteriAdi = m.MusteriAdi,
            Taraf = ch.IslemTuru
        }
    )
    .ToListAsync();

            // Eğer aynı SeferGelirID için 1’den fazla kayıt varsa (unique yoksa) ilkini alalım:
            var cariMap = cariList
                .GroupBy(x => x.SeferGelirID)
                .ToDictionary(g => g.Key, g => g.First());

            // 4) In-memory birleştir
            Gelirler = Gelirler
                .Select(x => cariMap.TryGetValue(x.SeferGelirID, out var ci)
                    ? x with { IsCarilestirildi = true, CariMusteriAdi = ci.MusteriAdi, CariIslemTuru = ci.Taraf }
                    : x)
                .ToList();

            GelirToplamlari = await _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.FirmaID == firmaId && g.SeferID == id)
                .GroupBy(g => g.ParaBirimi)
                .Select(g => new { PB = g.Key!, Sum = g.Sum(x => x.Tutar) })
                .ToDictionaryAsync(x => x.PB, x => x.Sum);

            IsClosed = (Data?.Durum ?? (byte)0) == 2;
            return Page();
        }

        // NOT: Razor Pages'ta method seviyesinde [ValidateAntiForgeryToken] gerekmez ve hata verir.
        // Formda @Html.AntiForgeryToken() zaten var.
        public async Task<IActionResult> OnPostMasrafSilAsync(int seferId, int id)
        {
            var firmaId = User.GetFirmaId();

            var masraf = await _context.SeferMasraflari
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId
                                       && x.SeferID == seferId
                                       && x.SeferMasrafID == id);

            if (masraf == null)
            {
                TempData["StatusMessage"] = "Masraf bulunamadı.";
                return RedirectToPage(new { id = seferId });
            }

            try
            {
                _context.SeferMasraflari.Remove(masraf);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Masraf silindi.";
            }
            catch (Exception ex)
            {
                TempData["StatusMessage"] = "Silme sırasında hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new { id = seferId });
        }

        public async Task<IActionResult> OnPostGelirSilAsync(int seferId, int id)
        {
            var firmaId = User.GetFirmaId();

            var gelir = await _context.SeferGelirleri
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SeferID == seferId && x.SeferGelirID == id);

            if (gelir == null)
            {
                TempData["StatusMessage"] = "Gelir bulunamadı.";
                return RedirectToPage(new { id = seferId });
            }

            try
            {
                _context.SeferGelirleri.Remove(gelir);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Gelir silindi.";
            }
            catch (Exception ex)
            {
                TempData["StatusMessage"] = "Silme sırasında hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new { id = seferId });
        }

        public async Task<IActionResult> OnPostGelirCariCikartAsync(int seferId, int gelirId)
        {
            var firmaId = User.GetFirmaId();

            var ch = await _context.CariHareketler
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SeferGelirID == gelirId);

            if (ch == null)
            {
                TempData["StatusMessage"] = "Cari hareket bulunamadı.";
                return RedirectToPage(new { id = seferId });
            }

            _context.CariHareketler.Remove(ch);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Cariye işleme kaldırıldı.";
            return RedirectToPage(new { id = seferId });
        }


        public async Task<IActionResult> OnPostGelireKaydetAsync(int seferId, int siparisId)
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            // Bu sipariş gerçekten bu sefere bağlı mı?
            var linkVarMi = await _context.SeferSevkiyatlar
                .AnyAsync(x => x.Sefer.FirmaID == firmaId
                            && x.SeferID == seferId
                            && x.Sevkiyat.SiparisID == siparisId);

            if (!linkVarMi)
            {
                TempData["StatusMessage"] = "Sipariş bu sefere bağlı değil.";
                return RedirectToPage(new { id = seferId });
            }

            // Sipariş bilgilerini al
            var s = await _context.Siparisler
                .Where(x => x.FirmaID == firmaId && x.SiparisID == siparisId)
                .Select(x => new { x.Tutar, x.ParaBirimi, x.YukAciklamasi, x.SiparisTarihi })
                .FirstOrDefaultAsync();

            if (s == null)
            {
                TempData["StatusMessage"] = "Sipariş bulunamadı.";
                return RedirectToPage(new { id = seferId });
            }

            var tutar = s.Tutar ?? 0m;
            var pb = string.IsNullOrWhiteSpace(s.ParaBirimi) ? "TL" : s.ParaBirimi!;
            var acik = $"Sipariş #{siparisId} - {s.YukAciklamasi}";

            // Aynı sipariş için daha önce gelir yazıldıysa güncelle
            var mevcut = await _context.SeferGelirleri
                .FirstOrDefaultAsync(g => g.FirmaID == firmaId
                                       && g.SeferID == seferId
                                       && g.IlgiliSiparisID == siparisId);

            if (mevcut is null)
            {
                _context.SeferGelirleri.Add(new SeferGelir
                {
                    FirmaID = firmaId,
                    KullaniciID = userId,
                    SeferID = seferId,
                    Tarih = s.SiparisTarihi.Date,
                    Aciklama = acik,
                    Tutar = tutar,
                    ParaBirimi = pb,
                    IlgiliSiparisID = siparisId,
                    Notlar = null,
                    CreatedAt = DateTime.Now
                });

                TempData["StatusMessage"] = "Gelir kaydedildi.";
            }
            else
            {
                mevcut.Tarih = s.SiparisTarihi.Date;
                mevcut.Aciklama = acik;
                mevcut.Tutar = tutar;
                mevcut.ParaBirimi = pb;
                TempData["StatusMessage"] = "Gelir güncellendi.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = seferId });
        }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostGelirCariyeIsleAsync(int seferId, int gelirId, int musteriId, string? aciklama)
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            // 1) Gelir var mı ve bu sefere mi ait?
            var gelir = await _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.FirmaID == firmaId && g.SeferGelirID == gelirId && g.SeferID == seferId)
                .Select(g => new { g.SeferGelirID, g.SeferID, g.Tutar, g.ParaBirimi, g.Tarih })
                .FirstOrDefaultAsync();

            if (gelir == null)
            {
                TempData["StatusMessage"] = "Gelir bulunamadı.";
                return RedirectToPage(new { id = seferId });
            }

            // 2) Bu gelir daha önce carileştirilmiş mi? (tekilleştirme)
            var already = await _context.CariHareketler
                .AnyAsync(ch => ch.FirmaID == firmaId && ch.SeferGelirID == gelirId);

            if (already)
            {
                TempData["StatusMessage"] = "Bu gelir zaten carileştirilmiş.";
                return RedirectToPage(new { id = seferId });
            }

            // 3) Müşteri gerçekten bu firmaya ait mi?
            var musteriVar = await _context.Musteriler
                .AnyAsync(m => m.FirmaID == firmaId && m.MusteriID == musteriId);

            if (!musteriVar)
            {
                TempData["StatusMessage"] = "Seçilen müşteri bulunamadı.";
                return RedirectToPage(new { id = seferId });
            }

            var seferInfo = await _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SeferID == seferId)
                .Select(s => new
                {
                    s.SeferKodu,
                    CekiciPlaka = s.Arac != null ? s.Arac.Plaka : null
                })
                .FirstOrDefaultAsync();

            var seferNoText = string.IsNullOrWhiteSpace(seferInfo?.SeferKodu) ? $"SF-{seferId}" : seferInfo!.SeferKodu!;
            var cekiciText = string.IsNullOrWhiteSpace(seferInfo?.CekiciPlaka) ? "" : $" - Çekici: {seferInfo!.CekiciPlaka}";


            var aciklamaFinal = $"Sefer No: {seferNoText}{cekiciText} - Sefer Geliri";
            if (!string.IsNullOrWhiteSpace(aciklama))
                aciklamaFinal = aciklama.Trim();


            // 5) Cari hareket oluştur
            var chNew = new CariHareket
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                MusteriID = musteriId,

                Tarih = DateTime.Now,                 // istersen gelir.Tarih yaparız
                ParaBirimi = gelir.ParaBirimi,
                Tutar = gelir.Tutar,

                Aciklama = aciklamaFinal,

                // Taraf/İşlem Türü sende "IslemTuru" alanı var
                IslemTuru = "Sefer Alacak",
                CreatedAt = DateTime.Now,
                CreatedByKullaniciID = userId,
                Yonu = 1,

                // Bağlantı
                SeferGelirID = gelirId
            };

            _context.CariHareketler.Add(chNew);

            try
            {
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Cariye işlendi.";
            }
            catch (Exception ex)
            {
                TempData["StatusMessage"] = "Kayıt sırasında hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new { id = seferId });
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostKapatAsync(int id, DateTime? kapanisTarihi)
        {
            var firmaId = User.GetFirmaId();

            var sefer = await _context.Seferler
                .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SeferID == id);

            if (sefer is null)
            {
                TempData["StatusMessage"] = "Sefer bulunamadı.";
                return RedirectToPage("./Index");
            }

            if (sefer.Durum == 2)
            {
                TempData["StatusMessage"] = "Sefer zaten kapalı.";
                return RedirectToPage(new { id });
            }

            var ts = kapanisTarihi ?? DateTime.Now; // kullanıcı seçmediyse şimdi
            sefer.DonusTarihi = ts;
            sefer.Durum = 2;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"Sefer kapatıldı ({ts:dd.MM.yyyy HH:mm}).";
            return RedirectToPage(new { id });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSiparisCikarAsync(int seferId, int siparisId)
        {
            var firmaId = User.GetFirmaId();

            // Kapalı sefer kontrolü (Durum=2 kapalı varsayımı)
            var seferDurum = await _context.Seferler
                .Where(s => s.FirmaID == firmaId && s.SeferID == seferId)
                .Select(s => s.Durum)
                .FirstOrDefaultAsync();

            if (seferDurum == 2)
            {
                TempData["StatusMessage"] = "Kapalı seferden sipariş çıkarılamaz.";
                return RedirectToPage(new { id = seferId });
            }

            // Bu sefer-sipariş bağlantıları
            var links = await _context.SeferSevkiyatlar
                .Where(ss => ss.SeferID == seferId
                          && ss.Sevkiyat.FirmaID == firmaId
                          && ss.Sevkiyat.SiparisID == siparisId)
                .ToListAsync();

            if (links.Count == 0)
            {
                TempData["StatusMessage"] = "Bu sefere bağlı böyle bir sipariş bağlantısı bulunamadı.";
                return RedirectToPage(new { id = seferId });
            }

            _context.SeferSevkiyatlar.RemoveRange(links);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"Sipariş #{siparisId} seferden çıkarıldı.";
            return RedirectToPage(new { id = seferId });
        }


    }
}
