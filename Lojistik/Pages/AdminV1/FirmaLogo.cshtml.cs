using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;

namespace Lojistik.Pages.AdminV1;

public class FirmaLogoModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public FirmaLogoModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // GET ile firma seçimi
    [BindProperty(SupportsGet = true)]
    public int? FirmaId { get; set; }

    // Dropdown
    public List<FirmaRow> Firmalar { get; set; } = new();

    public record FirmaRow(int FirmaID, string FirmaKodu, string FirmaAdi);
    [TempData]
    public string? ErrorText { get; set; }

    [TempData]
    public string? OkText { get; set; }

    // Mevcut logo
    public string? CurrentLogoUrl { get; set; }
    public string CacheBuster { get; set; } = "";

    // Upload
    [BindProperty]
    public IFormFile? Logo { get; set; }

    public async Task OnGetAsync()
    {
        await LoadFirmalar();

        if (FirmaId is null) return;

        var firma = await _db.Firmalar.AsNoTracking()
            .Where(x => x.FirmaID == FirmaId.Value)
            .Select(x => new { x.LogoYolu, x.UpdatedAt })
            .FirstOrDefaultAsync();

        if (firma == null)
        {
            // seçilen firma yoksa
            FirmaId = null;
            return;
        }

        CurrentLogoUrl = firma.LogoYolu;
        CacheBuster = (firma.UpdatedAt ?? DateTime.UtcNow).Ticks.ToString();
    }

    public async Task<IActionResult> OnPostAsync([FromForm] int? FirmaId)
    {
        this.FirmaId = FirmaId;

        try
        {
            await LoadFirmalar();

            if (!FirmaId.HasValue || FirmaId.Value <= 0)
            {
                ModelState.AddModelError(string.Empty, "Firma seçiniz.");
                return Page();
            }

            var firma = await _db.Firmalar.FirstOrDefaultAsync(x => x.FirmaID == FirmaId.Value);
            if (firma == null) return NotFound();

            if (Logo == null || Logo.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Logo seçiniz.");
                CacheBuster = (firma.UpdatedAt ?? DateTime.UtcNow).Ticks.ToString();
                return Page();
            }

            var ext = Path.GetExtension(Logo.FileName).ToLowerInvariant();
            var allowedExt = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            if (!allowedExt.Contains(ext))
            {
                ModelState.AddModelError(string.Empty, "Sadece PNG/JPG/WEBP yükleyin.");
                CacheBuster = (firma.UpdatedAt ?? DateTime.UtcNow).Ticks.ToString();
                return Page();
            }

            var mime = (Logo.ContentType ?? "").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(mime) || !mime.StartsWith("image/"))
            {
                ModelState.AddModelError(string.Empty, "Geçersiz dosya tipi.");
                CacheBuster = (firma.UpdatedAt ?? DateTime.UtcNow).Ticks.ToString();
                return Page();
            }

            // WebRoot fallback
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            // KLASÖR ADI: senin sistemde /uploads/logolar/ kullanýlýyor
            var relDir = Path.Combine("uploads", "logolar");
            var absDir = Path.Combine(webRoot, relDir);
            Directory.CreateDirectory(absDir);

            // dosya adý benzersiz
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var safeFileName = $"firma_{firma.FirmaID}_{stamp}{ext}";
            var absPath = Path.Combine(absDir, safeFileName);

            using (var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await Logo.CopyToAsync(fs);

            // DB update
            firma.LogoYolu = "/" + Path.Combine(relDir, safeFileName).Replace("\\", "/");
            firma.LogoDosyaAdi = Path.GetFileName(Logo.FileName);
            firma.LogoMimeType = mime;
            firma.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            OkText = $"Kaydedildi. FirmaID={firma.FirmaID} | Yol={firma.LogoYolu}";
            return RedirectToPage(new { firmaId = firma.FirmaID });
        }
        catch (Exception ex)
        {
            // gerçek hatayý ekranda gösterelim
            ErrorText = ex.GetType().Name + ": " + ex.Message;
            if (ex.InnerException != null)
                ErrorText += " | Inner: " + ex.InnerException.Message;

            return RedirectToPage(new { firmaId = FirmaId });
        }
    }


    private async Task LoadFirmalar()
    {
        Firmalar = await _db.Firmalar.AsNoTracking()
            .OrderBy(x => x.FirmaAdi)
            .Select(x => new FirmaRow(x.FirmaID, x.FirmaKodu, x.FirmaAdi))
            .ToListAsync();
    }
}
