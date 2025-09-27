// Pages/Siparisler/Index.cshtml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SiparisID,
            DateTime SiparisTarihi,
            string YukAciklamasi,
            string? Gonderen,
            string? Alici,
            string? AliciUlke,
            int? Adet,
            string? AdetCinsi,
            int? Kilo,
            decimal? Tutar,
            string? ParaBirimi,
            byte Durum,
            string DurumText,
            string? CekiciPlaka,
            string? DorsePlaka
        );

        public IList<Row> Items { get; set; } = new List<Row>();

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var raw = await _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId)
                .OrderByDescending(s => s.SiparisID)
                .Select(s => new
                {
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.YukAciklamasi,
                    Gonderen = s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    Alici = s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    AliciUlke = s.AliciMusteri != null && s.AliciMusteri.Ulke != null
                        ? s.AliciMusteri.Ulke.UlkeAdi
                        : null,
                    s.Adet,
                    s.AdetCinsi,
                    s.Kilo,
                    s.Tutar,
                    s.ParaBirimi,
                    s.Durum,

                    // son sevkiyatın araç/dorse plakaları
                    CekiciPlaka = s.Sevkiyatlar
                        .OrderByDescending(v => v.SevkiyatID)
                        .Select(v => v.Arac != null ? v.Arac.Plaka : null)
                        .FirstOrDefault(),
                    DorsePlaka = s.Sevkiyatlar
                        .OrderByDescending(v => v.SevkiyatID)
                        .Select(v => v.Dorse != null ? v.Dorse.Plaka : null)
                        .FirstOrDefault()
                })
                .ToListAsync();

            Items = raw
                .Select(x => new Row(
                    x.SiparisID,
                    x.SiparisTarihi,
                    x.YukAciklamasi,
                    x.Gonderen,
                    x.Alici,
                    x.AliciUlke,
                    x.Adet,
                    x.AdetCinsi,
                    x.Kilo,
                    x.Tutar,
                    x.ParaBirimi,
                    x.Durum,
                    GetDurumText(x.Durum),
                    x.CekiciPlaka,
                    x.DorsePlaka
                ))
                .ToList();
        }

        private static string GetDurumText(byte d) =>
            d switch
            {
                0 => "0 - Yeni",
                1 => "1 - Onaylı",
                2 => "2 - Hazırlanıyor",
                3 => "3 - Sevkte",
                4 => "4 - Tamamlandı",
                5 => "5 - İptal",
                _ => $"{d} - Bilinmiyor"
            };
    }
}
