using Lojistik.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Lojistik.Pages.Raporlar;

[Authorize]
public class KademeRaporuModel : PageModel
{
    private readonly AppDbContext _db;
    public KademeRaporuModel(AppDbContext db) => _db = db;

    // Filtreler
    [BindProperty(SupportsGet = true)]
    public int? AracId { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? Baslangic { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? Bitis { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? IslemFirmasiQ { get; set; }

    // ✅ yeni
    [BindProperty(SupportsGet = true)]
    public bool SadeceOdenmeyen { get; set; } = false;

    public List<SelectListItem> Araclar { get; set; } = new();
    public List<RowVm> Rows { get; set; } = new();
    public List<ToplamVm> Toplamlar { get; set; } = new();

    public class RowVm
    {
        public DateTime Tarih { get; set; }
        public string Plaka { get; set; } = "";
        public string YapilanIslem { get; set; } = "";
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; } = "";
        public string? Notlar { get; set; }
        public string? IslemFirmasi { get; set; }
        public int Odeme { get; set; } // ✅ yeni
    }

    public class ToplamVm
    {
        public string ParaBirimi { get; set; } = "";
        public decimal Toplam { get; set; }
    }

    public async Task OnGetAsync()
    {
        var firmaId = GetFirmaId();

        Araclar = await _db.Araclar
            .AsNoTracking()
            .Where(a => a.FirmaID == firmaId)
            .OrderBy(a => a.Plaka)
            .Select(a => new SelectListItem { Value = a.AracID.ToString(), Text = a.Plaka })
            .ToListAsync();

        var today = DateTime.Today;
        var from = Baslangic?.Date ?? new DateTime(today.Year, today.Month, 1);
        var to = Bitis?.Date ?? today;


        var q = _db.AracKademeler
            .AsNoTracking()
            .Include(k => k.Arac)
            .Where(k => k.Arac.FirmaID == firmaId);

        q = q.Where(k => k.Tarih >= from && k.Tarih <= to);

        if (AracId.HasValue)
            q = q.Where(k => k.AracID == AracId.Value);

        if (!string.IsNullOrWhiteSpace(Q))
            q = q.Where(k => k.YapilanIslem.Contains(Q));

        if (!string.IsNullOrWhiteSpace(IslemFirmasiQ))
            q = q.Where(k => k.IslemFirmasi != null && k.IslemFirmasi.Contains(IslemFirmasiQ));

        // ✅ sadece ödenmeyen
        if (SadeceOdenmeyen)
            q = q.Where(k => k.Odeme == 0);

        Rows = await q
            .OrderByDescending(k => k.Tarih)
            .ThenBy(k => k.KademeID)
            .Select(k => new RowVm
            {
                Tarih = k.Tarih,
                Plaka = k.Arac.Plaka,
                YapilanIslem = k.YapilanIslem,
                Tutar = k.Tutar,
                ParaBirimi = k.ParaBirimi,
                Notlar = k.Notlar,
                IslemFirmasi = k.IslemFirmasi,
                Odeme = k.Odeme
            })
            .ToListAsync();

        Toplamlar = await q
            .GroupBy(x => x.ParaBirimi)
            .Select(g => new ToplamVm
            {
                ParaBirimi = g.Key,
                Toplam = g.Sum(x => x.Tutar)
            })
            .OrderBy(x => x.ParaBirimi)
            .ToListAsync();
    }

    private int GetFirmaId()
    {
        var v = User.FindFirstValue("FirmaID") ?? User.FindFirstValue("FirmaId");
        if (int.TryParse(v, out var firmaId)) return firmaId;
        throw new InvalidOperationException("FirmaID claim bulunamadı.");
    }
}
