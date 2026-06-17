using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar.Sefer
{
    [Authorize]
    public class SeferRaporDetayModel : PageModel
    {
        private readonly AppDbContext _context;
        public SeferRaporDetayModel(AppDbContext context) => _context = context;

        public SeferItem? Data { get; set; }
        public List<SiparisRow> Siparisler { get; set; } = new();
        public List<SelectListItem> MusteriOptions { get; set; } = new(); // (kullanılmıyor ama istersen kaldır)
        public List<GelirRow> Gelirler { get; set; } = new();
        public List<MasrafRow> Masraflar { get; set; } = new();
        public Dictionary<string, decimal> MasrafToplamlari { get; set; } = new();
        public Dictionary<string, decimal> GelirToplamlari { get; set; } = new();

        [BindProperty(SupportsGet = false)]
        public int SeferId { get; set; }


        public record SeferItem(
            int SeferID,
            string? SeferKodu,
            string? AracPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            DateTime? CikisTarihi,
            DateTime? DonusTarihi,
            byte Durum,
            string? Notlar,
            DateTime CreatedAt
        );

        public record MasrafRow(
            int SeferMasrafID,
            DateTime Tarih,
            string MasrafTipi,
            decimal Tutar,
            string ParaBirimi,
            string? FaturaBelgeNo,
            string? Ulke,
            string? Yer,
            string? Notlar
        );

        public record GelirRow(
            int SeferGelirID,
            DateTime Tarih,
            decimal Tutar,
            string ParaBirimi,
            string? Aciklama,
            int? IlgiliSiparisID,
            string? Notlar,
            string? FaturaNo,
            string? CikisIl,
            string? VarisIl,
            bool IsCarilestirildi,
            string? CariMusteriAdi,
            string? CariIslemTuru
        );

        public record SiparisRow(
            int SiparisID,
            DateTime SiparisTarihi,
            string YukAciklamasi,
            string? Gonderen,
            string? Alici,
            string? AliciUlke,
            string? AliciSehir,
            decimal? Tutar,
            string? ParaBirimi,
            string? FaturaNo,
            string? DorsePlaka
        );

        public async Task<IActionResult> OnGetAsync()
        {
            // Direkt URL ile açılmasın
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var sube = User.GetAltSubeKodu(); // senin Seferler.SubeKodu buraya yazılıyor

            // Buradan sonrası senin mevcut OnGetAsync(id) içeriğin aynısı,
            // sadece id yerine SeferId kullanacağız ve sube filtresi ekleyeceğiz.

            var id = SeferId;
            Data = await _context.Seferler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SeferID == id)
                .Select(s => new SeferItem(
                    s.SeferID,
                    s.SeferKodu,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.CikisTarihi,
                    s.DonusTarihi,
                    s.Durum,
                    s.Notlar,
                    s.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");

            Siparisler = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .Where(x => x.Sefer.FirmaID == firmaId && x.SeferID == id)
                .Select(x => new SiparisRow(
                    x.Sevkiyat.Siparis.SiparisID,
                    x.Sevkiyat.Siparis.SiparisTarihi,
                    x.Sevkiyat.Siparis.YukAciklamasi,
                    x.Sevkiyat.Siparis.GonderenMusteri != null ? x.Sevkiyat.Siparis.GonderenMusteri.MusteriAdi : null,
                    x.Sevkiyat.Siparis.AliciMusteri != null ? x.Sevkiyat.Siparis.AliciMusteri.MusteriAdi : null,
                    x.Sevkiyat.Siparis.AliciMusteri != null && x.Sevkiyat.Siparis.AliciMusteri.Ulke != null
                        ? x.Sevkiyat.Siparis.AliciMusteri.Ulke.UlkeAdi : null,
                    x.Sevkiyat.Siparis.AliciMusteri != null && x.Sevkiyat.Siparis.AliciMusteri.Sehir != null
                        ? x.Sevkiyat.Siparis.AliciMusteri.Sehir.SehirAdi : null,
                    x.Sevkiyat.Siparis.Tutar,
                    x.Sevkiyat.Siparis.ParaBirimi,
                    x.Sevkiyat.Siparis.FaturaNo,
                    x.Sevkiyat.Dorse != null ? x.Sevkiyat.Dorse.Plaka : null
                ))
                .ToListAsync();

            Masraflar = await _context.SeferMasraflari
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId && m.SeferID == id)
                .OrderByDescending(m => m.Tarih)
                .Select(m => new MasrafRow(
                    m.SeferMasrafID,
                    m.Tarih,
                    m.MasrafTipi,
                    m.Tutar,
                    m.ParaBirimi,
                    m.FaturaBelgeNo,
                    m.Ulke,
                    m.Yer,
                    m.Notlar
                ))
                .ToListAsync();

            MasrafToplamlari = await _context.SeferMasraflari
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId && m.SeferID == id)
                .GroupBy(m => m.ParaBirimi)
                .Select(g => new { PB = g.Key!, Sum = g.Sum(x => x.Tutar) })
                .ToDictionaryAsync(x => x.PB, x => x.Sum);

            Gelirler = await _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.FirmaID == firmaId && g.SeferID == id)
                .OrderByDescending(g => g.Tarih)
                .Select(g => new GelirRow(
                    g.SeferGelirID,
                    g.Tarih,
                    g.Tutar,
                    g.ParaBirimi,
                    g.Aciklama,
                    g.IlgiliSiparisID,
                    g.Notlar,
                    _context.Siparisler
                        .Where(s => s.FirmaID == firmaId && s.SiparisID == g.IlgiliSiparisID)
                        .Select(s => s.FaturaNo)
                        .FirstOrDefault(),
                    g.CikisIl,
                    g.VarisIl,
                    false,
                    null,
                    null
                ))
                .ToListAsync();

            var gelirIds = Gelirler.Select(x => x.SeferGelirID).ToList();

            var cariList = await _context.CariHareketler
                .AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && ch.SeferGelirID != null && gelirIds.Contains(ch.SeferGelirID.Value))
                .Join(
                    _context.Musteriler.AsNoTracking().Where(m => m.FirmaID == firmaId),
                    ch => new { ch.FirmaID, ch.MusteriID },
                    m => new { m.FirmaID, m.MusteriID },
                    (ch, m) => new
                    {
                        SeferGelirID = ch.SeferGelirID!.Value,
                        MusteriAdi = m.MusteriAdi,
                        Taraf = ch.IslemTuru
                    }
                )
                .ToListAsync();

            var cariMap = cariList
                .GroupBy(x => x.SeferGelirID)
                .ToDictionary(g => g.Key, g => g.First());

            Gelirler = Gelirler
                .Select(x => cariMap.TryGetValue(x.SeferGelirID, out var ci)
                    ? x with { IsCarilestirildi = true, CariMusteriAdi = ci.MusteriAdi, CariIslemTuru = ci.Taraf }
                    : x)
                .ToList();

            GelirToplamlari = await _context.SeferGelirleri
                .AsNoTracking()
                .Where(g => g.FirmaID == firmaId && g.SeferID == id)
                .GroupBy(g => g.ParaBirimi)
                .Select(g => new { PB = g.Key!, Sum = g.Sum(x => x.Tutar) })
                .ToDictionaryAsync(x => x.PB, x => x.Sum);

            return Page();
        }
    }
}
