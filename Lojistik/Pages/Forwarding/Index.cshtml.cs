using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Forwarding
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public byte? durumFiltre { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? d1 { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? d2 { get; set; }

        public class Row
        {
            public int ForwardingID { get; set; }
            public string? DosyaNo { get; set; }
            public string? YukuVerenAdi { get; set; }
            public string? YukuAlanAdi { get; set; }
            public string? TasiyiciGorunum { get; set; }  // sistemdeki firma adı ya da serbest metin
            public string? YuklemeyeriAdi { get; set; }
            public string? VarisYeriAdi { get; set; }
            public DateTime? YuklemeTarihi { get; set; }
            public DateTime? TeslimTarihi { get; set; }
            public DateTime? GercekTeslimTarihi { get; set; }
            public byte Durum { get; set; }
            public decimal ToplamSatis { get; set; }
            public decimal ToplamAlis { get; set; }
            public string? SatisPB { get; set; }
            public string? AlisPB { get; set; }
            /// <summary>Kalem özeti: "2 Palet · 3 Koli" gibi</summary>
            public string? KalemOzet { get; set; }
        }

        public List<Row> Items { get; set; } = new();
        public int ToplamKayit { get; set; }

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var query = _context.ForwardingIsler
                .AsNoTracking()
                .Where(f => f.FirmaID == firmaId);

            if (durumFiltre.HasValue)
                query = query.Where(f => f.Durum == durumFiltre.Value);

            if (d1.HasValue)
                query = query.Where(f => f.YuklemeTarihi >= d1.Value.Date);

            if (d2.HasValue)
                query = query.Where(f => f.YuklemeTarihi <= d2.Value.Date);

            var raw = await query
                .OrderByDescending(f => f.ForwardingID)
                .Select(f => new
                {
                    f.ForwardingID,
                    f.DosyaNo,
                    YukuVerenAdi       = f.YukuVeren != null ? f.YukuVeren.MusteriAdi
                                       : f.Musteri != null  ? f.Musteri.MusteriAdi
                                       : null,
                    YukuAlanAdi        = f.YukuAlan != null ? f.YukuAlan.MusteriAdi : null,
                    TasiyiciGorunum    = f.TasiyiciMusteri != null ? f.TasiyiciMusteri.MusteriAdi : f.TasiyiciAdi,
                    f.YuklemeyeriAdi,
                    f.VarisYeriAdi,
                    f.YuklemeTarihi,
                    f.TeslimTarihi,
                    f.GercekTeslimTarihi,
                    f.Durum
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                raw = raw.Where(r =>
                    (r.YukuVerenAdi ?? "").ToLower().Contains(term) ||
                    (r.YukuAlanAdi ?? "").ToLower().Contains(term) ||
                    (r.DosyaNo ?? "").ToLower().Contains(term) ||
                    (r.TasiyiciGorunum ?? "").ToLower().Contains(term) ||
                    (r.YuklemeyeriAdi ?? "").ToLower().Contains(term) ||
                    (r.VarisYeriAdi ?? "").ToLower().Contains(term)
                ).ToList();
            }

            ToplamKayit = raw.Count;

            // Fiyat toplamları + Kalem özetleri
            var fwdIds = raw.Select(r => r.ForwardingID).ToList();
            var fiyatlar = await _context.ForwardingFiyatlar
                .AsNoTracking()
                .Where(f => f.FirmaID == firmaId && fwdIds.Contains(f.ForwardingID))
                .Select(f => new { f.ForwardingID, f.FiyatTuru, f.Tutar, f.ParaBirimi })
                .ToListAsync();

            var kalemler = await _context.ForwardingKalemler
                .AsNoTracking()
                .Where(k => k.FirmaID == firmaId && fwdIds.Contains(k.ForwardingID))
                .Select(k => new { k.ForwardingID, k.Adet, k.Nevi })
                .ToListAsync();

            Items = raw.Select(r =>
            {
                var fp        = fiyatlar.Where(x => x.ForwardingID == r.ForwardingID).ToList();
                var satisList = fp.Where(x => x.FiyatTuru == "Satis").ToList();
                var alisList  = fp.Where(x => x.FiyatTuru == "Alis").ToList();
                var kp        = kalemler.Where(x => x.ForwardingID == r.ForwardingID).ToList();
                var kalemOzet = kp.Any()
                    ? string.Join(" · ", kp.GroupBy(k => k.Nevi)
                        .Select(g =>
                        {
                            var toplam = g.Sum(k => k.Adet);
                            var adetStr = toplam % 1 == 0 ? toplam.ToString("N0") : toplam.ToString("N2");
                            return $"{adetStr} {g.Key}";
                        }))
                    : null;
                return new Row
                {
                    ForwardingID       = r.ForwardingID,
                    DosyaNo            = r.DosyaNo,
                    YukuVerenAdi       = r.YukuVerenAdi,
                    YukuAlanAdi        = r.YukuAlanAdi,
                    TasiyiciGorunum    = r.TasiyiciGorunum,
                    YuklemeyeriAdi     = r.YuklemeyeriAdi,
                    VarisYeriAdi       = r.VarisYeriAdi,
                    YuklemeTarihi      = r.YuklemeTarihi,
                    TeslimTarihi       = r.TeslimTarihi,
                    GercekTeslimTarihi = r.GercekTeslimTarihi,
                    Durum              = r.Durum,
                    KalemOzet          = kalemOzet,
                    ToplamSatis        = satisList.Sum(x => x.Tutar),
                    ToplamAlis         = alisList.Sum(x => x.Tutar),
                    SatisPB            = satisList.GroupBy(x => x.ParaBirimi).OrderByDescending(g => g.Sum(z => z.Tutar)).FirstOrDefault()?.Key,
                    AlisPB             = alisList.GroupBy(x => x.ParaBirimi).OrderByDescending(g => g.Sum(z => z.Tutar)).FirstOrDefault()?.Key,
                };
            }).ToList();
        }
    }
}
