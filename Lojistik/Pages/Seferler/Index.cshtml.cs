using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Seferler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SeferID,
            string? SeferKodu,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi, // kullanmıyoruz ama şimdilik dursun
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            byte Durum,
            string? Ulke,
            string? Sehir
        );

        public IList<Row> Items { get; set; } = new List<Row>();

        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public string? sort { get; set; } = "cikis_desc";
        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int pageSize { get; set; } = 20;
        [BindProperty(SupportsGet = true)] public byte? durum { get; set; }
        [BindProperty(SupportsGet = true)] public bool ShowClosed { get; set; }   // Kapalıları listele togglesı

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / pageSize);

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            var query = _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            if (ShowClosed)
                query = query.Where(s => s.Durum == 2);
            else
                query = query.Where(s => s.Durum != 2);

            if (durum.HasValue)
                query = query.Where(s => s.Durum == durum.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(s =>
                    (s.SeferKodu != null && s.SeferKodu.Contains(term)) ||
                    (s.SurucuAdi != null && s.SurucuAdi.Contains(term)) ||
                    (s.Arac != null && s.Arac.Plaka.Contains(term)) ||
                    (s.Dorse != null && s.Dorse.Plaka.Contains(term))
                );
            }

            query = sort switch
            {
                "cikis_asc" => query.OrderBy(s => s.CikisTarihi).ThenByDescending(s => s.SeferID),
                "cikis_desc" => query.OrderByDescending(s => s.CikisTarihi).ThenByDescending(s => s.SeferID),
                "kod_asc" => query.OrderBy(s => s.SeferKodu),
                "kod_desc" => query.OrderByDescending(s => s.SeferKodu),
                _ => query.OrderByDescending(s => s.CikisTarihi).ThenByDescending(s => s.SeferID)
            };

            TotalCount = await query.CountAsync();
            Items = await query
                .Select(s => new Row(
                    s.SeferID,
                    s.SeferKodu,
                    s.CikisTarihi,
                      s.DonusTarihi,   
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.Durum,
                    // Ülke
                    s.SeferSevkiyatlar
                        .OrderByDescending(xx => xx.SevkiyatID)
                        .Select(xx => xx.Sevkiyat.Siparis.AliciMusteri.Ulke.UlkeAdi)
                        .FirstOrDefault(),
                    // Şehir
                    s.SeferSevkiyatlar
                        .OrderByDescending(xx => xx.SevkiyatID)
                        .Select(xx => xx.Sevkiyat.Siparis.AliciMusteri.Sehir.SehirAdi)
                        .FirstOrDefault()
                ))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var yetki2 = await _context.Kullanicilar
        .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
        .Select(k => k.YetkiSeviyesi2)
        .FirstOrDefaultAsync();

            ViewData["Yetki2"] = yetki2;
        }
    }
}
