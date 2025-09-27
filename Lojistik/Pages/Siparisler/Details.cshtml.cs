using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public bool HasSevkiyat { get; set; } = false;

        public record Item(
            int SiparisID,
            DateTime SiparisTarihi,
            string YukAciklamasi,
            int GonderenMusteriID,
            string? Gonderen,
            string? GonderenUlke,
            string? GonderenSehir,
            int AliciMusteriID,
            string? Alici,
            string? AliciUlke,
            string? AliciSehir,
            int? AraTedarikciMusteriID,
            string? AraTedarikci,
            int? Adet,
            string? AdetCinsi,
            int? Kilo,
            decimal? Tutar,
            string? ParaBirimi,
            string? FaturaNo,
            string? SubeKodu,
            string? Notlar,
            byte Durum,
            DateTime CreatedAt
        );

        public record SevkiyatRow(
            int SevkiyatID,
            string? DorsePlaka,
            DateTime? PlanlananYuklemeTarihi,
            DateTime? YuklemeTarihi,
            DateTime? VarisTarihi,
            byte Durum
        );

        public record SeferRow(
            int SeferID,
            string? SeferKodu,
            DateTime? CikisTarihi,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            byte Durum
        );

        public Item? Data { get; set; }
        public List<SevkiyatRow> Sevkiyatlar { get; set; } = new();
        public List<SeferRow> Seferler { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SiparisID == id)
                .Select(s => new Item(
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.YukAciklamasi,
                    s.GonderenMusteriID,
                    s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    s.GonderenMusteri != null ? s.GonderenMusteri.Ulke!.UlkeAdi : null,
                    s.GonderenMusteri != null ? s.GonderenMusteri.Sehir!.SehirAdi : null,
                    s.AliciMusteriID,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.Ulke!.UlkeAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.Sehir!.SehirAdi : null,
                    s.AraTedarikciMusteriID,
                    s.AraTedarikciMusteri != null ? s.AraTedarikciMusteri.MusteriAdi : null,
                    s.Adet,
                    s.AdetCinsi,
                    s.Kilo,
                    s.Tutar,
                    s.ParaBirimi,
                    s.FaturaNo,
                    s.SubeKodu,
                    s.Notlar,
                    s.Durum,
                    s.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");

            // İlişkili Sevkiyatlar
            Sevkiyatlar = await _context.Sevkiyatlar
                .AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.SiparisID == id)
                .OrderByDescending(x => x.SevkiyatID)
                .Select(x => new SevkiyatRow(
                    x.SevkiyatID,
                    x.Dorse != null ? x.Dorse.Plaka : null,
                    x.PlanlananYuklemeTarihi,
                    x.YuklemeTarihi,
                    x.VarisTarihi,
                    x.Durum
                ))
                .ToListAsync();

            HasSevkiyat = Sevkiyatlar.Any();

            // İlişkili Seferler (SeferSevkiyatlar üzerinden)
            var seferFlat = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.Sevkiyat.SiparisID == id)
                .Select(x => new
                {
                    x.Sefer.SeferID,
                    x.Sefer.SeferKodu,
                    x.Sefer.CikisTarihi,
                    Cekici = x.Sefer.Arac != null ? x.Sefer.Arac.Plaka : null,
                    Dorse = x.Sefer.Dorse != null ? x.Sefer.Dorse.Plaka : null,
                    x.Sefer.SurucuAdi,
                    x.Sefer.Durum
                })
                .ToListAsync();

            Seferler = seferFlat
                .GroupBy(a => a.SeferID)
                .Select(g =>
                {
                    var f = g.First();
                    return new SeferRow(
                        f.SeferID, f.SeferKodu, f.CikisTarihi,
                        f.Cekici, f.Dorse, f.SurucuAdi, f.Durum
                    );
                })
                .OrderByDescending(r => r.SeferID)
                .ToList();

            return Page();
        }
    }
}
