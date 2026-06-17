using Lojistik.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lojistik.Pages.FirmaPanel.Kullanicilar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    public IndexModel(AppDbContext context) => _context = context;

    public record KullaniciSatir(
        int     KullaniciID,
        string  Username,
        string? KullaniciAdi,
        bool    IsActive,
        bool    IsFirmaAdmin,
        byte    SiparisYetkisi,
        byte    ForwardingYetkisi,
        byte    AracYetkisi,
        byte    SeferYetkisi,
        byte    MusteriYetkisi,
        byte    CariYetkisi,
        byte    RaporYetkisi
    );

    public List<KullaniciSatir> Kullanicilar { get; set; } = new();
    public int    AktifSayisi      { get; set; }
    public int    ToplamSayisi     { get; set; }
    public int    KullaniciLimiti  { get; set; }
    public string? Hata            { get; set; }
    public string? Basari          { get; set; }

    // ── Claim yardımcıları ────────────────────────────────────────────────
    private int? ClaimFirmaId() =>
        int.TryParse(User.FindFirst("FirmaID")?.Value, out var id) ? id : null;

    private int ClaimKullaniciId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    // ── GET ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync()
    {
        var firmaId = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        Hata   = TempData["Hata"]   as string;
        Basari = TempData["Basari"] as string;

        await YukleAsync(firmaId.Value);
        return Page();
    }

    // ── POST: Aktif/Pasif Toggle ──────────────────────────────────────────
    public async Task<IActionResult> OnPostToggleAktifAsync(int kullaniciId)
    {
        var firmaId   = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        var loginId = ClaimKullaniciId();

        // Kullanıcıyı bul; başka firma veya sistem admin ise NotFound
        var k = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.KullaniciID == kullaniciId
                                   && x.FirmaID     == firmaId.Value
                                   && !x.IsSistemAdmin);
        if (k == null) return NotFound();

        if (k.IsActive)
        {
            // ── Aktif → Pasif geçişinde korumalar ────────────────────────

            // Kendi hesabını pasif yapamaz
            if (kullaniciId == loginId)
            {
                TempData["Hata"] = "Kendi hesabınızı pasif yapamazsınız.";
                return RedirectToPage();
            }

            // Son aktif firma admin koruması
            if (k.IsFirmaAdmin)
            {
                int aktifAdminSayisi = await _context.Kullanicilar
                    .CountAsync(x => x.FirmaID     == firmaId.Value
                                  && x.IsActive
                                  && x.IsFirmaAdmin
                                  && !x.IsSistemAdmin);
                if (aktifAdminSayisi <= 1)
                {
                    TempData["Hata"] = "Firmada en az bir aktif firma yöneticisi kalmalıdır.";
                    return RedirectToPage();
                }
            }
        }
        else
        {
            // ── Pasif → Aktif geçişinde limit kontrolü ────────────────────
            int limit = await GetKullaniciLimitiAsync(firmaId.Value);
            int aktifSayi = await _context.Kullanicilar
                .CountAsync(x => x.FirmaID == firmaId.Value
                              && x.IsActive
                              && !x.IsSistemAdmin);
            if (aktifSayi >= limit)
            {
                TempData["Hata"] = "Bu firmanın kullanıcı limiti dolmuştur. Yeni aktif kullanıcı oluşturulamaz.";
                return RedirectToPage();
            }
        }

        k.IsActive  = !k.IsActive;
        k.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Basari"] = k.IsActive ? "Kullanıcı aktifleştirildi." : "Kullanıcı pasife alındı.";
        return RedirectToPage();
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────
    private async Task YukleAsync(int firmaId)
    {
        Kullanicilar = await _context.Kullanicilar
            .AsNoTracking()
            .Where(k => k.FirmaID == firmaId && !k.IsSistemAdmin)
            .OrderByDescending(k => k.IsFirmaAdmin)
            .ThenBy(k => k.KullaniciAdi)
            .Select(k => new KullaniciSatir(
                k.KullaniciID, k.Username, k.KullaniciAdi,
                k.IsActive, k.IsFirmaAdmin,
                k.SiparisYetkisi, k.ForwardingYetkisi, k.AracYetkisi,
                k.SeferYetkisi, k.MusteriYetkisi, k.CariYetkisi, k.RaporYetkisi))
            .ToListAsync();

        AktifSayisi    = Kullanicilar.Count(k => k.IsActive);
        ToplamSayisi   = Kullanicilar.Count;
        KullaniciLimiti = await GetKullaniciLimitiAsync(firmaId);
    }

    // FirmaKodu köprüsü üzerinden ProgramFirmalar.KullaniciLimiti'ni al
    private async Task<int> GetKullaniciLimitiAsync(int firmaId)
    {
        var firmaKodu = await _context.Firmalar.AsNoTracking()
            .Where(f => f.FirmaID == firmaId)
            .Select(f => f.FirmaKodu)
            .FirstOrDefaultAsync();
        if (firmaKodu == null) return 0;

        return await _context.ProgramFirmalar.AsNoTracking()
            .Where(p => p.FirmaKodu == firmaKodu)
            .Select(p => p.KullaniciLimiti)
            .FirstOrDefaultAsync();
    }
}
