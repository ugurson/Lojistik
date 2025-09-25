// Pages/Sevkiyatlar/Details.cshtml.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Sevkiyatlar
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public record Item(
            int SevkiyatID,
            string? SevkiyatKodu,
            int SiparisID,
            string? SiparisYukAciklamasi,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            string? YuklemeMusteri,
            string? BosaltmaMusteri,
            string? YuklemeAdres,
            string? BosaltmaAdres,
            string? YuklemeNoktasi,
            string? BosaltmaNoktasi,
            DateTime? PlanlananYuklemeTarihi,
            DateTime? YuklemeTarihi,
            DateTime? GumrukCikisTarihi,
            DateTime? VarisTarihi,
            string? CMRNo,
            string? MRN,
            byte Durum,
            string? Notlar,
            DateTime CreatedAt
        );

        public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.Sevkiyatlar
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SevkiyatID == id)
                .Select(s => new Item(
                    s.SevkiyatID,
                    s.SevkiyatKodu,
                    s.SiparisID,
                    s.Siparis != null ? s.Siparis.YukAciklamasi : null,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.YuklemeMusteri != null ? s.YuklemeMusteri.MusteriAdi : null,
                    s.BosaltmaMusteri != null ? s.BosaltmaMusteri.MusteriAdi : null,
                    s.YuklemeAdres,
                    s.BosaltmaAdres,
                    s.YuklemeNoktasi,
                    s.BosaltmaNoktasi,
                    s.PlanlananYuklemeTarihi,
                    s.YuklemeTarihi,
                    s.GumrukCikisTarihi,
                    s.VarisTarihi,
                    s.CMRNo,
                    s.MRN,
                    s.Durum,
                    s.Notlar,
                    s.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }
    }
}
