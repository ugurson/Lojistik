// Pages/Sevkiyatlar/Index.cshtml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Sevkiyatlar
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SevkiyatID,
            string? SevkiyatKodu,
            int SiparisID,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            DateTime? PlanlananYuklemeTarihi,
            DateTime? YuklemeTarihi,
            DateTime? VarisTarihi,
            string? YuklemeMusteri,
            string? BosaltmaMusteri,
            byte Durum
        );

        public IList<Row> Items { get; set; } = new List<Row>();

        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public byte? durum { get; set; }
        [BindProperty(SupportsGet = true)] public int? siparisId { get; set; }
        [BindProperty(SupportsGet = true)] public string? sort { get; set; } = "yukleme_desc";
        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int pageSize { get; set; } = 20;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / pageSize);

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var query = _context.Sevkiyatlar
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            if (siparisId.HasValue)
                query = query.Where(s => s.SiparisID == siparisId.Value);

            if (durum.HasValue)
                query = query.Where(s => s.Durum == durum.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(s =>
                    (s.SevkiyatKodu != null && s.SevkiyatKodu.Contains(term)) ||
                    (s.SurucuAdi != null && s.SurucuAdi.Contains(term)) ||
                    (s.CMRNo != null && s.CMRNo.Contains(term)) ||
                    (s.MRN != null && s.MRN.Contains(term)) ||
                    (s.Arac != null && s.Arac.Plaka.Contains(term)) ||
                    (s.Dorse != null && s.Dorse.Plaka.Contains(term)) ||
                    (s.YuklemeMusteri != null && s.YuklemeMusteri.MusteriAdi.Contains(term)) ||
                    (s.BosaltmaMusteri != null && s.BosaltmaMusteri.MusteriAdi.Contains(term))
                );
            }

            query = sort switch
            {
                "yukleme_asc" => query.OrderBy(s => s.YuklemeTarihi).ThenByDescending(s => s.SevkiyatID),
                "yukleme_desc" => query.OrderByDescending(s => s.YuklemeTarihi).ThenByDescending(s => s.SevkiyatID),
                "varis_asc" => query.OrderBy(s => s.VarisTarihi).ThenByDescending(s => s.SevkiyatID),
                "varis_desc" => query.OrderByDescending(s => s.VarisTarihi).ThenByDescending(s => s.SevkiyatID),
                "kod_asc" => query.OrderBy(s => s.SevkiyatKodu),
                "kod_desc" => query.OrderByDescending(s => s.SevkiyatKodu),
                _ => query.OrderByDescending(s => s.YuklemeTarihi).ThenByDescending(s => s.SevkiyatID)
            };

            TotalCount = await query.CountAsync();

            Items = await query
                .Select(s => new Row(
                    s.SevkiyatID,
                    s.SevkiyatKodu,
                    s.SiparisID,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.PlanlananYuklemeTarihi,
                    s.YuklemeTarihi,
                    s.VarisTarihi,
                    s.YuklemeMusteri != null ? s.YuklemeMusteri.MusteriAdi : null,
                    s.BosaltmaMusteri != null ? s.BosaltmaMusteri.MusteriAdi : null,
                    s.Durum
                ))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
