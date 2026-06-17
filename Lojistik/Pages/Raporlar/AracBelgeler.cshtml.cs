using Lojistik.Data;
using Lojistik.Extensions; // GetFirmaId()
using Lojistik.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Raporlar;

[Authorize]
public class AracBelgelerModel : PageModel
{
    private readonly AppDbContext _context;
    public AracBelgelerModel(AppDbContext context) => _context = context;

    public IList<AracBelgesi> Belgeler { get; set; } = new List<AracBelgesi>();

    // ---- Filtreler (URL'den okunur) ----
    [BindProperty(SupportsGet = true)] public string? Plaka { get; set; }
    [BindProperty(SupportsGet = true)] public string? BelgeTipi { get; set; }
    [BindProperty(SupportsGet = true)] public string? BelgeNo { get; set; }
    [BindProperty(SupportsGet = true)] public string? Firma { get; set; }

    // ÖNEMLÝ: select name="pb" ile birebir ayný olmalý
    [BindProperty(SupportsGet = true)] public string? pb { get; set; }

    [BindProperty(SupportsGet = true)] public string? Notlar { get; set; }

    // ---- Sýralama ----
    [BindProperty(SupportsGet = true)] public string? SortField { get; set; } = "Plaka";
    [BindProperty(SupportsGet = true)] public string? SortDir { get; set; } = "asc";

    // ---- Sayfalama ----
    [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1; // 1-based
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 40;

    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(PageSize, 1));

    public async Task OnGetAsync()
    {
        if (PageIndex < 1) PageIndex = 1;
        if (PageSize is < 1 or > 200) PageSize = 40;

        int firmaId = User.GetFirmaId();

        IQueryable<AracBelgesi> query = _context.AracBelgeleri
            .Include(x => x.Arac)
            .Where(x => x.Arac != null && x.Arac.FirmaID == firmaId);

        // Filtreler
        if (!string.IsNullOrWhiteSpace(Plaka))
            query = query.Where(x => x.Arac != null && EF.Functions.Like(x.Arac.Plaka, $"%{Plaka}%"));

        if (!string.IsNullOrWhiteSpace(BelgeTipi))
            query = query.Where(x => EF.Functions.Like(x.BelgeTipi ?? "", $"%{BelgeTipi}%"));

        if (!string.IsNullOrWhiteSpace(BelgeNo))
            query = query.Where(x => EF.Functions.Like(x.BelgeNo ?? "", $"%{BelgeNo}%"));

        if (!string.IsNullOrWhiteSpace(Firma))
            query = query.Where(x => EF.Functions.Like(x.Firma ?? "", $"%{Firma}%"));

        // PARA BÝRÝMÝ (pb) - düzgün çalýþmasý için normalize + eþitlik
        if (!string.IsNullOrWhiteSpace(pb))
        {
            var pbn = pb.Trim().ToUpperInvariant();
            query = query.Where(x => (x.ParaBirimi ?? "").ToUpper() == pbn);
        }

        if (!string.IsNullOrWhiteSpace(Notlar))
            query = query.Where(x => EF.Functions.Like(x.Notlar ?? "", $"%{Notlar}%"));

        TotalCount = await query.CountAsync();

        bool desc = string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (SortField?.ToLowerInvariant()) switch
        {
            "plaka" => desc ? query.OrderByDescending(x => x.Arac!.Plaka) : query.OrderBy(x => x.Arac!.Plaka),
            "belgetipi" => desc ? query.OrderByDescending(x => x.BelgeTipi) : query.OrderBy(x => x.BelgeTipi),
            "belgeno" => desc ? query.OrderByDescending(x => x.BelgeNo) : query.OrderBy(x => x.BelgeNo),
            "firma" => desc ? query.OrderByDescending(x => x.Firma) : query.OrderBy(x => x.Firma),
            "baslangictarihi" => desc ? query.OrderByDescending(x => x.BaslangicTarihi) : query.OrderBy(x => x.BaslangicTarihi),
            "bitistarihi" => desc ? query.OrderByDescending(x => x.BitisTarihi) : query.OrderBy(x => x.BitisTarihi),
            "tutar" => desc ? query.OrderByDescending(x => x.Tutar) : query.OrderBy(x => x.Tutar),
            "parabirimi" => desc ? query.OrderByDescending(x => x.ParaBirimi) : query.OrderBy(x => x.ParaBirimi),
            "notlar" => desc ? query.OrderByDescending(x => x.Notlar) : query.OrderBy(x => x.Notlar),
            _ => desc ? query.OrderByDescending(x => x.Arac!.Plaka) : query.OrderBy(x => x.Arac!.Plaka),
        };

        if (TotalPages > 0 && PageIndex > TotalPages) PageIndex = TotalPages;
        int skip = (PageIndex - 1) * PageSize;

        Belgeler = await query.AsNoTracking()
                              .Skip(skip)
                              .Take(PageSize)
                              .ToListAsync();
    }
}