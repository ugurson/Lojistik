using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar.Kullanicilar;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    public EditModel(AppDbContext context) => _context = context;

    public ProgramFirma? Firma { get; set; }

    [BindProperty] public int     KullaniciID      { get; set; }
    [BindProperty] public int     FirmaID          { get; set; }
    [BindProperty] public string  KullaniciAdi     { get; set; } = "";
    [BindProperty] public string  Username         { get; set; } = "";
    [BindProperty] public string  Password         { get; set; } = "";
    [BindProperty] public bool    IsActive         { get; set; }
    [BindProperty] public bool    IsFirmaAdmin     { get; set; }
    [BindProperty] public bool    IsSistemAdmin    { get; set; }
    [BindProperty] public byte    SiparisYetkisi   { get; set; }
    [BindProperty] public byte    ForwardingYetkisi{ get; set; }
    [BindProperty] public byte    AracYetkisi      { get; set; }
    [BindProperty] public byte    SeferYetkisi     { get; set; }
    [BindProperty] public byte    MusteriYetkisi   { get; set; }
    [BindProperty] public byte    CariYetkisi      { get; set; }
    [BindProperty] public byte    RaporYetkisi     { get; set; }

    public string? Hata { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool WasActive { get; set; }   // orijinal IsActive (pasiften aktife geçiş limiti için)

    public async Task<IActionResult> OnGetAsync(int id, int firmaId)
    {
        var k = await _context.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(x => x.KullaniciID == id && x.FirmaID == firmaId);
        if (k == null) return NotFound();

        Firma = await _context.ProgramFirmalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == firmaId);
        if (Firma == null) return NotFound();

        KullaniciID       = k.KullaniciID;
        FirmaID           = k.FirmaID;
        KullaniciAdi      = k.KullaniciAdi ?? "";
        Username          = k.Username;
        Password          = k.Password;
        IsActive          = k.IsActive;
        IsFirmaAdmin      = k.IsFirmaAdmin;
        IsSistemAdmin     = k.IsSistemAdmin;
        SiparisYetkisi    = k.SiparisYetkisi;
        ForwardingYetkisi = k.ForwardingYetkisi;
        AracYetkisi       = k.AracYetkisi;
        SeferYetkisi      = k.SeferYetkisi;
        MusteriYetkisi    = k.MusteriYetkisi;
        CariYetkisi       = k.CariYetkisi;
        RaporYetkisi      = k.RaporYetkisi;
        CreatedAt         = k.CreatedAt;
        WasActive         = k.IsActive;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Firma = await _context.ProgramFirmalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == FirmaID);
        if (Firma == null) return NotFound();

        if (string.IsNullOrWhiteSpace(KullaniciAdi) ||
            string.IsNullOrWhiteSpace(Username)     ||
            string.IsNullOrWhiteSpace(Password))
        {
            Hata = "Ad Soyad, Kullanıcı Adı ve Şifre zorunludur.";
            CreatedAt = (await _context.Kullanicilar.AsNoTracking()
                .Where(k => k.KullaniciID == KullaniciID)
                .Select(k => k.CreatedAt).FirstOrDefaultAsync());
            return Page();
        }

        // Username benzersizlik (aynı firmada, başka kullanıcıda)
        bool usernameVarMi = await _context.Kullanicilar
            .AnyAsync(k => k.FirmaID == FirmaID && k.Username == Username.Trim()
                        && k.KullaniciID != KullaniciID);
        if (usernameVarMi)
        {
            Hata = $"'{Username}' kullanıcı adı bu firmada zaten kullanılıyor.";
            CreatedAt = (await _context.Kullanicilar.AsNoTracking()
                .Where(k => k.KullaniciID == KullaniciID)
                .Select(k => k.CreatedAt).FirstOrDefaultAsync());
            return Page();
        }

        // Pasiften aktife geçerken limit kontrolü
        var mevcutIsActive = await _context.Kullanicilar.AsNoTracking()
            .Where(k => k.KullaniciID == KullaniciID)
            .Select(k => k.IsActive)
            .FirstOrDefaultAsync();

        if (IsActive && !mevcutIsActive)
        {
            int aktifSayi = await _context.Kullanicilar
                .CountAsync(k => k.FirmaID == FirmaID && k.IsActive && !k.IsSistemAdmin);
            if (aktifSayi >= Firma.KullaniciLimiti)
            {
                Hata = $"Kullanıcı limiti dolu ({Firma.KullaniciLimiti} aktif kullanıcı). " +
                       "Bu kullanıcı aktifleştirilemez.";
                CreatedAt = (await _context.Kullanicilar.AsNoTracking()
                    .Where(k => k.KullaniciID == KullaniciID)
                    .Select(k => k.CreatedAt).FirstOrDefaultAsync());
                return Page();
            }
        }

        var kullanici = await _context.Kullanicilar.FindAsync(KullaniciID);
        if (kullanici == null || kullanici.FirmaID != FirmaID) return NotFound();

        kullanici.KullaniciAdi      = KullaniciAdi.Trim();
        kullanici.Username          = Username.Trim();
        kullanici.Password          = Password;
        kullanici.IsActive          = IsActive;
        kullanici.IsFirmaAdmin      = IsFirmaAdmin;
        kullanici.IsSistemAdmin     = IsSistemAdmin;
        kullanici.SiparisYetkisi    = SiparisYetkisi;
        kullanici.ForwardingYetkisi = ForwardingYetkisi;
        kullanici.AracYetkisi       = AracYetkisi;
        kullanici.SeferYetkisi      = SeferYetkisi;
        kullanici.MusteriYetkisi    = MusteriYetkisi;
        kullanici.CariYetkisi       = CariYetkisi;
        kullanici.RaporYetkisi      = RaporYetkisi;
        kullanici.UpdatedAt         = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToPage("Index", new { firmaId = FirmaID });
    }
}
