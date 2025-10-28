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
            string? DorsePlaka,
            decimal? Tutar,
            string? ParaBirimi,
            byte Durum,
            string? SeferAracPlaka,
            string? SeferSurucuAdi,
            string? SeferKodu
        );

        public IList<Row> Items { get; set; } = new List<Row>();
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
                var term = q.Trim();
                query = query.Where(s =>
                    (s.YukAciklamasi != null && s.YukAciklamasi.Contains(term)) ||
                    (s.GonderenMusteri != null && s.GonderenMusteri.MusteriAdi.Contains(term)) ||
                    (s.AliciMusteri != null && s.AliciMusteri.MusteriAdi.Contains(term)) ||
                    s.SiparisID.ToString().Contains(term) ||
                    (s.FaturaNo != null && s.FaturaNo.Contains(term))
                );
            }

            query = query.OrderByDescending(s => s.SiparisTarihi).ThenByDescending(s => s.SiparisID);

            TotalCount = await query.CountAsync();

            Items = await query
                .Select(s => new Row(
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    s.AliciMusteri != null && s.AliciMusteri.Ulke != null ? s.AliciMusteri.Ulke.UlkeAdi : null,
                    s.AliciMusteri != null && s.AliciMusteri.Sehir != null ? s.AliciMusteri.Sehir.SehirAdi : null,
                    _context.Sevkiyatlar
                        .Where(x => x.FirmaID == firmaId && x.SiparisID == s.SiparisID && x.DorseID != null)
                        .OrderByDescending(x => x.SevkiyatID)
                        .Select(x => x.Dorse!.Plaka)
                        .FirstOrDefault(),
                    s.Tutar,
                    s.ParaBirimi,
                    s.Durum,
                    _context.SeferSevkiyatlar
                        .Where(ss => ss.Sevkiyat.SiparisID == s.SiparisID && ss.Sevkiyat.FirmaID == firmaId)
                        .OrderByDescending(ss => ss.SeferSevkiyatID)
                        .ThenByDescending(ss => ss.SeferID)
                        .Select(ss => ss.Sefer.Arac != null ? ss.Sefer.Arac.Plaka : null)
                        .FirstOrDefault(),
                    _context.SeferSevkiyatlar
                        .Where(ss => ss.Sevkiyat.SiparisID == s.SiparisID && ss.Sevkiyat.FirmaID == firmaId)
                        .OrderByDescending(ss => ss.SeferSevkiyatID)
                        .ThenByDescending(ss => ss.SeferID)
                        .Select(ss => ss.Sefer.SurucuAdi)
                        .FirstOrDefault(),
                    _context.SeferSevkiyatlar
                        .Where(ss => ss.Sevkiyat.SiparisID == s.SiparisID && ss.Sevkiyat.FirmaID == firmaId)
                        .OrderByDescending(ss => ss.SeferSevkiyatID)
                        .ThenByDescending(ss => ss.SeferID)
                        .Select(ss => ss.Sefer.SeferKodu)
                        .FirstOrDefault()
                ))
                .Skip((this.page - 1) * this.pageSize)
                .Take(this.pageSize)
                .ToListAsync();
        }
    }
}
