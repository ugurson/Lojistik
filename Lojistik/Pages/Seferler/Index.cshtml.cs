// Pages/Seferler/Index.cshtml.cs
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
            DateTime? DonusTarihi,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            byte Durum
        );

        public IList<Row> Items { get; set; } = new List<Row>();

        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public string? sort { get; set; } = "cikis_desc";
        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int pageSize { get; set; } = 20;
        [BindProperty(SupportsGet = true)] public byte? durum { get; set; }

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / pageSize);

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var query = _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

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
                "donus_asc" => query.OrderBy(s => s.DonusTarihi).ThenByDescending(s => s.SeferID),
                "donus_desc" => query.OrderByDescending(s => s.DonusTarihi).ThenByDescending(s => s.SeferID),
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
                    s.Durum
                ))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
