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
            byte Durum
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
                    x.Sevkiyat.Siparis.Durum
                ))
                .ToListAsync();

            return Page();
        }

        public static string GetDurumBadgeClass(byte durum) =>
            durum switch
            {
                0 => "secondary",   // Yeni
                1 => "info",        // Onaylı
                2 => "primary",     // Hazırlanıyor
                3 => "warning",     // Sevkte
                4 => "success",     // Tamamlandı
                5 => "dark",        // İptal
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
