// Pages/Raporlar/Yakit/Index.cshtml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar.Yakit
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            DateTime Tarih,
            string? SeferKodu,
            string? CekiciPlaka,
            string? SurucuAdi,
            string? Yer,
            decimal Litre,
            decimal Tutar,
            string PB
        );

        // Filtreler
        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? Start { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? yer { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; } // sefer kodu / plaka / sürücü

        // Özetler
        public Dictionary<string, decimal> ToplamTutarPB { get; set; } = new();
        public decimal ToplamLitre { get; set; }

        public IList<Row> Items { get; set; } = new List<Row>();

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var today = DateTime.Today;
            Start ??= new DateTime(today.Year, today.Month, 1);
            End ??= today;

            // Yakýt masraflarý (firma + tarih)
            var baseQ =
                from m in _context.SeferMasraflari.AsNoTracking()
                join s in _context.Seferler.AsNoTracking() on m.SeferID equals s.SeferID
                where m.FirmaID == firmaId
                      && s.FirmaID == firmaId
                      && m.MasrafTipi == "Yakýt"
                      && m.Tarih >= Start.Value.Date
                      && m.Tarih < End.Value.Date.AddDays(1)
                select new
                {
                    m.Tarih,
                    m.Tutar,
                    PB = (m.ParaBirimi ?? "TL").Trim().ToUpper(),
                    Litre = (decimal?)(m.YakitLitre ?? 0m),
                    Yer = m.Yer,
                    s.SeferKodu,
                    CekiciPlaka = s.Arac != null ? s.Arac.Plaka : null,
                    s.SurucuAdi
                };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                baseQ = baseQ.Where(x =>
                    (x.SeferKodu != null && x.SeferKodu.Contains(term)) ||
                    (x.CekiciPlaka != null && x.CekiciPlaka.Contains(term)) ||
                    (x.SurucuAdi != null && x.SurucuAdi.Contains(term))
                );
            }
            if (!string.IsNullOrWhiteSpace(yer))
            {
                var y = yer.Trim();
                baseQ = baseQ.Where(x => x.Yer != null && x.Yer.Contains(y));
            }

            // Liste
            var list = await baseQ
                .OrderByDescending(x => x.Tarih)
                .ThenByDescending(x => x.SeferKodu)
                .ToListAsync();

            Items = list.Select(x => new Row(
                x.Tarih,
                x.SeferKodu,
                x.CekiciPlaka,
                x.SurucuAdi,
                   x.Yer,
                x.Litre ?? 0m,
                x.Tutar,
                x.PB
            )).ToList();

            // Özet: toplam litre (PB baðýmsýz)
            ToplamLitre = Items.Sum(x => x.Litre);

            // Özet: PB bazýnda toplam tutar
            ToplamTutarPB = Items
                .GroupBy(x => x.PB)
                .ToDictionary(g => g.Key, g => g.Sum(z => z.Tutar));
        }
    }
}
