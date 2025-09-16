using Lojistik.Data;
using Lojistik.Models;
using Lojistik.Extensions;                 // GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Araclar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    public IndexModel(AppDbContext context) => _context = context;

    public IList<Arac> Araclar { get; set; } = new List<Arac>();

    // ---- Filtreler (URL'den okunur) ----
    [BindProperty(SupportsGet = true)] public string? Plaka { get; set; }
    [BindProperty(SupportsGet = true)] public string? Marka { get; set; }
    [BindProperty(SupportsGet = true)] public string? ModelAdi { get; set; }   // "Model" ile çakışmasın
    [BindProperty(SupportsGet = true)] public int? ModelYili { get; set; }
    [BindProperty(SupportsGet = true)] public string? AracTipi { get; set; }
    [BindProperty(SupportsGet = true)] public bool? IsDorse { get; set; }
    [BindProperty(SupportsGet = true)] public string? Durum { get; set; }
    [BindProperty(SupportsGet = true)] public string? Notlar { get; set; }

    // ---- Sıralama ----
    [BindProperty(SupportsGet = true)] public string? SortField { get; set; } = "Plaka";
    [BindProperty(SupportsGet = true)] public string? SortDir { get; set; } = "asc"; // asc | desc

    // ---- Sayfalama ----
    [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1; // 1-based
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 40;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(PageSize, 1));

    public async Task OnGetAsync()
    {
        // Güvenlik/sınırlar
        if (PageIndex < 1) PageIndex = 1;
        if (PageSize is < 1 or > 200) PageSize = 40;

        int firmaId = User.GetFirmaId();

        // 1) DAİMA firma filtresiyle başla
        IQueryable<Arac> query = _context.Araclar
                                         .Where(a => a.FirmaID == firmaId);

        // 2) Filtreler
        if (!string.IsNullOrWhiteSpace(Plaka))
            query = query.Where(a => EF.Functions.Like(a.Plaka, $"%{Plaka}%"));

        if (!string.IsNullOrWhiteSpace(Marka))
            query = query.Where(a => EF.Functions.Like(a.Marka ?? "", $"%{Marka}%"));

        if (!string.IsNullOrWhiteSpace(ModelAdi))
            query = query.Where(a => EF.Functions.Like(a.Model ?? "", $"%{ModelAdi}%"));

        if (ModelYili.HasValue)
            query = query.Where(a => a.ModelYili == ModelYili.Value);

        if (!string.IsNullOrWhiteSpace(AracTipi))
            query = query.Where(a => EF.Functions.Like(a.AracTipi ?? "", $"%{AracTipi}%"));

        if (IsDorse.HasValue)
            query = query.Where(a => a.IsDorse == IsDorse.Value);

        if (!string.IsNullOrWhiteSpace(Durum))
            query = query.Where(a => a.Durum == Durum);

        if (!string.IsNullOrWhiteSpace(Notlar))
            query = query.Where(a => EF.Functions.Like(a.Notlar ?? "", $"%{Notlar}%"));

        // 3) Toplam ve sıralama
        TotalCount = await query.CountAsync();

        bool desc = string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (SortField?.ToLowerInvariant()) switch
        {
            "plaka" => desc ? query.OrderByDescending(a => a.Plaka) : query.OrderBy(a => a.Plaka),
            "marka" => desc ? query.OrderByDescending(a => a.Marka) : query.OrderBy(a => a.Marka),
            "model" => desc ? query.OrderByDescending(a => a.Model) : query.OrderBy(a => a.Model),
            "modelyili" => desc ? query.OrderByDescending(a => a.ModelYili) : query.OrderBy(a => a.ModelYili),
            "aractipi" => desc ? query.OrderByDescending(a => a.AracTipi) : query.OrderBy(a => a.AracTipi),
            "isdorse" => desc ? query.OrderByDescending(a => a.IsDorse) : query.OrderBy(a => a.IsDorse),
            "durum" => desc ? query.OrderByDescending(a => a.Durum) : query.OrderBy(a => a.Durum),
            "notlar" => desc ? query.OrderByDescending(a => a.Notlar) : query.OrderBy(a => a.Notlar),
            _ => desc ? query.OrderByDescending(a => a.Plaka) : query.OrderBy(a => a.Plaka)
        };

        if (TotalPages > 0 && PageIndex > TotalPages) PageIndex = TotalPages;
        int skip = (PageIndex - 1) * PageSize;

        // 4) Son veri
        Araclar = await query.AsNoTracking()
                             .Skip(skip)
                             .Take(PageSize)
                             .ToListAsync();
    }
}
