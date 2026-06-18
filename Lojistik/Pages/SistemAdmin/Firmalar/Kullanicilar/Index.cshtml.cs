using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar.Kullanicilar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    public IndexModel(AppDbContext context) => _context = context;

    public Firma? Firma { get; set; }

    public record KullaniciSatir(
        int     KullaniciID,
        string  Username,
        string? KullaniciAdi,
        bool    IsActive,
        bool    IsFirmaAdmin,
        bool    IsSistemAdmin,
        byte    SiparisYetkisi,
        byte    ForwardingYetkisi
    );

    public List<KullaniciSatir> Kullanicilar { get; set; } = new();
    public int AktifSayisi  { get; set; }
    public int ToplamSayisi { get; set; }

    public async Task<IActionResult> OnGetAsync(int firmaId)
    {
        Firma = await _context.Firmalar
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == firmaId);

        if (Firma == null) return NotFound();

        Kullanicilar = await _context.Kullanicilar
            .AsNoTracking()
            .Where(k => k.FirmaID == firmaId)
            .OrderBy(k => k.KullaniciAdi)
            .Select(k => new KullaniciSatir(
                k.KullaniciID,
                k.Username,
                k.KullaniciAdi,
                k.IsActive,
                k.IsFirmaAdmin,
                k.IsSistemAdmin,
                k.SiparisYetkisi,
                k.ForwardingYetkisi
            ))
            .ToListAsync();

        AktifSayisi  = Kullanicilar.Count(k => k.IsActive);
        ToplamSayisi = Kullanicilar.Count;

        return Page();
    }

    // Aktif/Pasif toggle
    public async Task<IActionResult> OnPostToggleAktifAsync(int kullaniciId, int firmaId)
    {
        var k = await _context.Kullanicilar.FindAsync(kullaniciId);
        if (k == null || k.FirmaID != firmaId) return NotFound();

        // Pasiften aktife geçerken limit kontrolü
        if (!k.IsActive)
        {
            var firma = await _context.Firmalar.AsNoTracking()
                .FirstOrDefaultAsync(f => f.FirmaID == firmaId);
            if (firma != null)
            {
                var aktifSayi = await _context.Kullanicilar
                    .CountAsync(x => x.FirmaID == firmaId && x.IsActive && !x.IsSistemAdmin);
                if (aktifSayi >= (firma.KullaniciLimiti ?? 0))
                {
                    TempData["Hata"] = $"Kullanıcı limiti dolu ({firma.KullaniciLimiti}). Aktifleştirilemez.";
                    return RedirectToPage(new { firmaId });
                }
            }
        }

        k.IsActive  = !k.IsActive;
        k.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return RedirectToPage(new { firmaId });
    }
}
