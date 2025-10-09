using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class SonlananModel : PageModel
    {
        private readonly AppDbContext _context;
        public SonlananModel(AppDbContext context) => _context = context;

        public record Row(
            int SiparisID,
            DateTime SiparisTarihi,
            string? Gonderen,
            string? Alici,
            string? AliciUlke,
            string? AliciSehir,
            decimal? Tutar,
            string? ParaBirimi,
            byte Durum
        );

        public IList<Row> Items { get; set; } = new List<Row>();

        // basit arama & sayfa boyutu (istersen genişletebilirsin)
        public string? q { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / pageSize);

        public async Task OnGetAsync(string? q, int page = 1, int pageSize = 20)
        {
            this.q = q;
            this.page = Math.Max(1, page);
            this.pageSize = Math.Clamp(pageSize, 10, 100);

            var firmaId = User.GetFirmaId();

            var query = _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.Durum == 7); // sadece sonlandırılanlar

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(s =>
                    s.YukAciklamasi.Contains(q) ||
                    s.GonderenMusteri!.MusteriAdi.Contains(q) ||
                    s.AliciMusteri!.MusteriAdi.Contains(q) ||
                    (s.FaturaNo != null && s.FaturaNo.Contains(q)));
            }

            TotalCount = await query.CountAsync();

            Items = await query
                .OrderByDescending(s => s.SiparisTarihi).ThenByDescending(s => s.SiparisID)
                .Skip((this.page - 1) * this.pageSize)
                .Take(this.pageSize)
                .Select(s => new Row(
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.Ulke!.UlkeAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.Sehir!.SehirAdi : null,
                    s.Tutar,
                    s.ParaBirimi,
                    s.Durum
                ))
                .ToListAsync();
        }
    }
}
