// Pages/SeferGelirleri/Index.cshtml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SeferGelirleri
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SeferGelirID,
            DateTime Tarih,
            string? Aciklama,
            decimal Tutar,
            string ParaBirimi,
            int SeferID,
            string? SeferKodu,
            int? IlgiliSiparisID
        );

        public IList<Row> Items { get; set; } = new List<Row>();

        [BindProperty(SupportsGet = true)] public int? seferId { get; set; }
        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public string? sort { get; set; } = "tarih_desc";
        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int pageSize { get; set; } = 20;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / pageSize);

        public class ToplamRow { public string ParaBirimi { get; set; } = ""; public decimal Toplam { get; set; } }
        public List<ToplamRow> Toplamlar { get; set; } = new();

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var query = _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.FirmaID == firmaId);

            if (seferId.HasValue)
                query = query.Where(g => g.SeferID == seferId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(g =>
                    (g.Aciklama != null && g.Aciklama.Contains(term)) ||
                    g.ParaBirimi.Contains(term) ||
                    (g.IlgiliSiparisID != null && g.IlgiliSiparisID.ToString()!.Contains(term)) ||
                    (g.Sefer != null && (g.Sefer.SeferKodu ?? ("SF-" + g.SeferID)).Contains(term))
                );
            }

            query = sort switch
            {
                "tarih_asc" => query.OrderBy(g => g.Tarih),
                "tutar_asc" => query.OrderBy(g => g.Tutar),
                "tutar_desc" => query.OrderByDescending(g => g.Tutar),
                "acik_asc" => query.OrderBy(g => g.Aciklama),
                "acik_desc" => query.OrderByDescending(g => g.Aciklama),
                _ => query.OrderByDescending(g => g.Tarih)
            };

            TotalCount = await query.CountAsync();

            Items = await query
                .Select(g => new Row(
                    g.SeferGelirID,
                    g.Tarih,
                    g.Aciklama,
                    g.Tutar,
                    g.ParaBirimi,
                    g.SeferID,
                    g.Sefer != null ? g.Sefer.SeferKodu : null,
                    g.IlgiliSiparisID
                ))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Toplamlar = await _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.FirmaID == firmaId &&
                            (!seferId.HasValue || g.SeferID == seferId.Value) &&
                            (string.IsNullOrWhiteSpace(q) ||
                             (g.Aciklama != null && g.Aciklama.Contains(q!)) ||
                             g.ParaBirimi.Contains(q!) ||
                             (g.IlgiliSiparisID != null && g.IlgiliSiparisID.ToString()!.Contains(q!)) ||
                             (g.Sefer != null && (g.Sefer.SeferKodu ?? ("SF-" + g.SeferID)).Contains(q!))
                            ))
                .GroupBy(g => g.ParaBirimi)
                .Select(g => new ToplamRow { ParaBirimi = g.Key, Toplam = g.Sum(x => x.Tutar) })
                .OrderBy(t => t.ParaBirimi)
                .ToListAsync();
        }
    }
}
