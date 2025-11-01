// Pages/Raporlar/Sefer/Index.cshtml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar.Sefer
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SeferID,
            string? SeferKodu,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            bool? Ozmal
        );

        // ---- Filtreler / Params ----
        [BindProperty(SupportsGet = true)] public DateTime? Start { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public bool ozmal { get; set; } = true; // öncelik özmal

        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int pageSize { get; set; } = 20;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / pageSize);
        public IList<Row> Items { get; set; } = new List<Row>();

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var query = _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            // Tarih aralığı (Çıkış Tarihi)
            if (Start.HasValue) query = query.Where(s => s.CikisTarihi >= Start.Value.Date);
            if (End.HasValue) query = query.Where(s => s.CikisTarihi < End.Value.Date.AddDays(1));

            // Arama
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

            // Özmal (Arac.Ozmal alanı INT(0/1) varsayıldı)
            query = query.Where(s => s.Arac != null && s.Arac.Ozmal == (ozmal ? 1 : 0));

            // Sıralama
            query = query.OrderByDescending(s => s.CikisTarihi).ThenByDescending(s => s.SeferID);

            // Sayfalama metrikleri
            TotalCount = await query.CountAsync();

            Items = await query
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(Math.Clamp(pageSize, 10, 100))
                .Select(s => new Row(
                    s.SeferID,
                    s.SeferKodu,
                    s.CikisTarihi,
                    s.DonusTarihi,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.Arac != null ? (bool?)(s.Arac.Ozmal == 1) : null
                ))
                .ToListAsync();
        }
    }
}
