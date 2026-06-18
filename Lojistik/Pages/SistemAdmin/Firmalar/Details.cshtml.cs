using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context) => _context = context;

    public Firma? Firma { get; set; }

    public record ModulDurum(
        int      ModulID,
        string   ModulKodu,
        string   ModulAdi,
        bool     AtanmisVar,
        bool     IsActive,
        DateOnly? BaslamaTarihi,
        DateOnly? BitisTarihi
    );

    public List<ModulDurum> Moduller { get; set; } = new();

    public record KullaniciSatir(
        int     KullaniciID,
        string  Username,
        string? KullaniciAdi,
        bool    IsActive,
        bool    IsSistemAdmin
    );

    public List<KullaniciSatir> Kullanicilar { get; set; } = new();
    public int AktifKullaniciSayisi { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Firma = await _context.Firmalar
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == id);

        if (Firma == null) return NotFound();

        Kullanicilar = await _context.Kullanicilar
            .AsNoTracking()
            .Where(k => k.FirmaID == id)
            .OrderBy(k => k.KullaniciAdi)
            .Select(k => new KullaniciSatir(
                k.KullaniciID,
                k.Username,
                k.KullaniciAdi,
                k.IsActive,
                k.IsSistemAdmin
            ))
            .ToListAsync();

        // IsSistemAdmin kullanıcılar limite dahil edilmez
        AktifKullaniciSayisi = Kullanicilar.Count(k => k.IsActive && !k.IsSistemAdmin);

        // ── Modüller ─────────────────────────────────────────────────────
        var tumModuller = await _context.ProgramModulleri
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SiraNo).ThenBy(m => m.ModulAdi)
            .ToListAsync();

        var firmaModuller = await _context.ProgramFirmaModulleri
            .AsNoTracking()
            .Where(x => x.FirmaID == id)
            .ToListAsync();

        Moduller = tumModuller.Select(m =>
        {
            var fm = firmaModuller.FirstOrDefault(x => x.ModulID == m.ModulID);
            return new ModulDurum(
                m.ModulID,
                m.ModulKodu,
                m.ModulAdi,
                AtanmisVar:    fm != null,
                IsActive:      fm?.IsActive ?? false,
                BaslamaTarihi: fm?.BaslamaTarihi,
                BitisTarihi:   fm?.BitisTarihi
            );
        }).ToList();

        return Page();
    }
}
