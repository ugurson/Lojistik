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
            int AliciMusteriID,
            string? Alici,
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

        public Item? Data { get; set; }
        public List<SevkiyatRow> Sevkiyatlar { get; set; } = new();

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
                    s.AliciMusteriID,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
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

            return Page();
        }
    }
}
