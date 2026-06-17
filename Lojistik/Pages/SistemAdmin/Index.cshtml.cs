using Lojistik.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    // ── Dashboard metrikleri ──────────────────────────────────────────────
    public int ToplamFirmaSayisi    { get; set; }
    public int AktifFirmaSayisi     { get; set; }
    public int PasifFirmaSayisi     { get; set; }
    public int DemoFirmaSayisi      { get; set; }
    public int ToplamAktifKullanici { get; set; }
    public int LimitDolmusFirma     { get; set; }

    public List<FirmaSatir> SonFirmalar { get; set; } = new();

    public record FirmaSatir(
        int    FirmaID,
        string FirmaKodu,
        string FirmaAdi,
        string? PaketAdi,
        bool   IsActive,
        bool   DemoMu,
        int    KullaniciLimiti,
        int    AktifKullaniciSayisi,
        DateOnly? BitisTarihi
    );

    public async Task OnGetAsync()
    {
        var firmalar = await _context.ProgramFirmalar
            .AsNoTracking()
            .ToListAsync();

        ToplamFirmaSayisi = firmalar.Count;
        AktifFirmaSayisi  = firmalar.Count(f => f.IsActive);
        PasifFirmaSayisi  = firmalar.Count(f => !f.IsActive);
        DemoFirmaSayisi   = firmalar.Count(f => f.DemoMu);

        // Firma başına aktif kullanıcı sayısı (Kullanicilar tablosundan)
        var kullaniciSayilari = await _context.Kullanicilar
            .AsNoTracking()
            .Where(k => k.IsActive)
            .GroupBy(k => k.FirmaID)
            .Select(g => new { FirmaID = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.FirmaID, x => x.Sayi);

        ToplamAktifKullanici = kullaniciSayilari.Values.Sum();

        LimitDolmusFirma = firmalar.Count(f =>
            kullaniciSayilari.TryGetValue(f.FirmaID, out var s) && s >= f.KullaniciLimiti);

        // Son eklenen 10 firma
        SonFirmalar = firmalar
            .OrderByDescending(f => f.CreatedAt)
            .Take(10)
            .Select(f => new FirmaSatir(
                f.FirmaID,
                f.FirmaKodu,
                f.FirmaAdi,
                f.PaketAdi,
                f.IsActive,
                f.DemoMu,
                f.KullaniciLimiti,
                kullaniciSayilari.GetValueOrDefault(f.FirmaID, 0),
                f.BitisTarihi
            ))
            .ToList();
    }
}
