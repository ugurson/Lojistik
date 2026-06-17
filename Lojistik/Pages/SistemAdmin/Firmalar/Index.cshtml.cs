using Lojistik.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    public IndexModel(AppDbContext context) => _context = context;

    // ── Filtre ───────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public string? Arama     { get; set; }
    [BindProperty(SupportsGet = true)] public string? Durum     { get; set; }  // "aktif" | "pasif" | ""
    [BindProperty(SupportsGet = true)] public string? DemoFiltre{ get; set; }  // "demo"  | ""

    // ── Satır modeli ─────────────────────────────────────────────────────
    public record FirmaSatir(
        int     FirmaID,
        string  FirmaKodu,
        string  FirmaAdi,
        string? PaketAdi,
        bool    IsActive,
        bool    DemoMu,
        int     KullaniciLimiti,
        int     AktifKullaniciSayisi,
        decimal? AylikUcret,
        string  ParaBirimi,
        DateOnly BaslamaTarihi,
        DateOnly? BitisTarihi
    );

    public List<FirmaSatir> Firmalar { get; set; } = new();

    public async Task OnGetAsync()
    {
        var q = _context.ProgramFirmalar.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Arama))
            q = q.Where(f => f.FirmaAdi.Contains(Arama) || f.FirmaKodu.Contains(Arama));

        if (Durum == "aktif") q = q.Where(f => f.IsActive);
        if (Durum == "pasif") q = q.Where(f => !f.IsActive);
        if (DemoFiltre == "demo") q = q.Where(f => f.DemoMu);

        var firmalar = await q.OrderBy(f => f.FirmaAdi).ToListAsync();

        // Firma başına aktif kullanıcı sayısı
        var aktifSayilari = await _context.Kullanicilar
            .AsNoTracking()
            .Where(k => k.IsActive)
            .GroupBy(k => k.FirmaID)
            .Select(g => new { FirmaID = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.FirmaID, x => x.Sayi);

        Firmalar = firmalar.Select(f => new FirmaSatir(
            f.FirmaID,
            f.FirmaKodu,
            f.FirmaAdi,
            f.PaketAdi,
            f.IsActive,
            f.DemoMu,
            f.KullaniciLimiti,
            aktifSayilari.GetValueOrDefault(f.FirmaID, 0),
            f.AylikUcret,
            f.ParaBirimi,
            f.BaslamaTarihi,
            f.BitisTarihi
        )).ToList();
    }

    // ── Pasife / Aktife Al (inline POST) ─────────────────────────────────
    public async Task<IActionResult> OnPostToggleAktifAsync(int id)
    {
        var firma = await _context.ProgramFirmalar.FindAsync(id);
        if (firma == null) return NotFound();

        firma.IsActive  = !firma.IsActive;
        firma.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return RedirectToPage(new { Arama, Durum, DemoFiltre });
    }
}
