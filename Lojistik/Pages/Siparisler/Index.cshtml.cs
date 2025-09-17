using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;          // User.GetFirmaId(), GetSubeKodu(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        // Basit arama
        public string? Q { get; set; }

        // Liste satırı için küçük DTO
        public record Row(
            int SiparisID,
            string SiparisTarihi,
            string YukAciklamasi,
            string? Gonderen,
            string? Alici,
            string? AliciUlke,
            string? AliciSehir,
            int? Kilo,
            decimal? Tutar,
            string? ParaBirimi,
            string? FaturaNo
        );

        public List<Row> Items { get; set; } = new();

        public async Task OnGetAsync(string? q)
        {
            ViewData["Title"] = "Siparişler";
            Q = q;

            var firmaId = User.GetFirmaId();

            // Önce firmaya ait müşteri sözlüğü (ID->Unvan)
            var musteriDict = await _context.Musteriler
                .Where(m => m.FirmaID == firmaId)
                .Select(m => new { m.MusteriID, m.MusteriAdi })
                .ToDictionaryAsync(x => x.MusteriID, x => x.MusteriAdi);

            // Siparişler sorgusu (firma filtresi + basit arama)
            var query = _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qNorm = q.Trim();
                query = query.Where(s =>
                       s.YukAciklamasi.Contains(qNorm)
                    || (s.FaturaNo != null && s.FaturaNo.Contains(qNorm)));
            }

            var list = await query
                .Include(s => s.AliciMusteri)
                    .ThenInclude(m => m.Sehir)
                        .ThenInclude(se => se.Ulke)
                .OrderByDescending(s => s.SiparisID)
                .Select(s => new
                {
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.YukAciklamasi,
                    s.GonderenMusteriID,
                    s.AliciMusteriID,

                    // NULL-GÜVENLİ: EF Core sol join olarak çevirir
                    AliciUlke = s.AliciMusteri == null || s.AliciMusteri.Sehir == null || s.AliciMusteri.Sehir.Ulke == null
                        ? null
                        : s.AliciMusteri.Sehir.Ulke.UlkeAdi,
                    AliciSehir = s.AliciMusteri == null || s.AliciMusteri.Sehir == null
                        ? null
                        : s.AliciMusteri.Sehir.SehirAdi,

                    s.Kilo,
                    s.Tutar,
                    s.ParaBirimi,
                    s.FaturaNo
                })
                .ToListAsync();

            Items = list.Select(s => new Row(
    s.SiparisID,
    s.SiparisTarihi.ToString("dd-MM-yyyy"),
    s.YukAciklamasi,
    musteriDict.TryGetValue(s.GonderenMusteriID, out var g) ? g : null,
    musteriDict.TryGetValue(s.AliciMusteriID, out var a) ? a : null,
    s.AliciUlke,
    s.AliciSehir,
    s.Kilo,
    s.Tutar,
    s.ParaBirimi,
    s.FaturaNo
)).ToList();


        }
    }
}
