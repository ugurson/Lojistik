using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar.SiparisCari
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public record Row(
            int SiparisID,
            DateTime Tarih,
            string? Gonderen,
            string? Alici,
            string? AliciUlke,
            string? AliciSehir,
            decimal? Tutar,
            string? ParaBirimi,
            byte Durum,
            bool IsCariles,                 // cariye işlenmiş mi
            string? CariHesap,              // kimin carisine işlendi
            string? CariEvrakNo,
            DateTime? CariIslemTarihi
        );

        // ---- Filtreler ----
        [BindProperty(SupportsGet = true), DataType(DataType.Date)]
        public DateTime? Start { get; set; }

        [BindProperty(SupportsGet = true), DataType(DataType.Date)]
        public DateTime? End { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }                // sipno/müşteri/evrak

        // "" | "islendi" | "islenmedi"
        [BindProperty(SupportsGet = true)]
        public string? CariDurum { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PB { get; set; }

        public List<Row> Items { get; set; } = new();

        // Özetler
        public int CountIslendi { get; set; }
        public int CountIslenmedi { get; set; }
        public decimal SumIslendi { get; set; }
        public decimal SumIslenmedi { get; set; }

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var qSip = _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            if (Start.HasValue) qSip = qSip.Where(s => s.SiparisTarihi.Date >= Start.Value.Date);
            if (End.HasValue) qSip = qSip.Where(s => s.SiparisTarihi.Date <= End.Value.Date);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                qSip = qSip.Where(s =>
                    s.SiparisID.ToString().Contains(term) ||
                    (s.GonderenMusteri != null && s.GonderenMusteri.MusteriAdi.Contains(term)) ||
                    (s.AliciMusteri != null && s.AliciMusteri.MusteriAdi.Contains(term)) ||
                    (s.CariEvrakNo != null && s.CariEvrakNo.Contains(term))
                );
            }

            if (!string.IsNullOrWhiteSpace(PB))
                qSip = qSip.Where(s => s.ParaBirimi == PB);

            // Özetler (filtreli kümede)
            CountIslendi = await qSip.Where(s => s.IsCariles).CountAsync();
            CountIslenmedi = await qSip.Where(s => !s.IsCariles).CountAsync();
            SumIslendi = await qSip.Where(s => s.IsCariles).Select(s => s.Tutar ?? 0).SumAsync();
            SumIslenmedi = await qSip.Where(s => !s.IsCariles).Select(s => s.Tutar ?? 0).SumAsync();

            if (CariDurum == "islendi") qSip = qSip.Where(s => s.IsCariles);
            else if (CariDurum == "islenmedi") qSip = qSip.Where(s => !s.IsCariles);

            Items = await qSip
                .OrderByDescending(s => s.SiparisTarihi)
                .ThenByDescending(s => s.SiparisID)
                .Select(s => new Row(
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    s.AliciMusteri != null && s.AliciMusteri.Ulke != null ? s.AliciMusteri.Ulke.UlkeAdi : null,
                    s.AliciMusteri != null && s.AliciMusteri.Sehir != null ? s.AliciMusteri.Sehir.SehirAdi : null,
                    s.Tutar,
                    s.ParaBirimi,
                    s.Durum,
                    s.IsCariles,
                    s.CariIslenenMusteriID != null
                        ? _context.Musteriler.Where(m => m.MusteriID == s.CariIslenenMusteriID).Select(m => m.MusteriAdi).FirstOrDefault()
                        : null,
                    s.CariEvrakNo,
                    s.CariIslemTarihi
                ))
                .Take(1000)
                .ToListAsync();
        }
    }
}
