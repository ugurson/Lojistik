using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Seferler
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public SeferItem? Data { get; set; }
        public List<SiparisRow> Siparisler { get; set; } = new();

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
        public List<MasrafRow> Masraflar { get; set; } = new();
        public Dictionary<string, decimal> MasrafToplamlari { get; set; } = new();

        // DURUM KALDIRILDI, FATURANO EKLENDİ
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
            string? FaturaNo
        );

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostMasrafSilAsync(int seferId, int id)
        {
            var firmaId = User.GetFirmaId();

            // Masraf gerçekten bu firmaya ve bu sefere mi ait?
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
                    x.Sevkiyat.Siparis.FaturaNo   // <<< EKLENDİ
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

            return Page();
        }

        // (Aşağıdaki yardımcılar kalsa da olur; artık kullanılmıyorlar)
        public static string GetDurumBadgeClass(byte durum) =>
            durum switch
            {
                0 => "secondary",
                1 => "info",
                2 => "primary",
                3 => "warning",
                4 => "success",
                5 => "dark",
                _ => "secondary"
            };

        public static string GetDurumText(byte durum) =>
            durum switch
            {
                0 => "0 - Yeni",
                1 => "1 - Onaylı",
                2 => "2 - Hazırlanıyor",
                3 => "3 - Sevkte",
                4 => "4 - Tamamlandı",
                5 => "5 - İptal",
                _ => $"{durum} - Bilinmiyor"
            };
    }
}
