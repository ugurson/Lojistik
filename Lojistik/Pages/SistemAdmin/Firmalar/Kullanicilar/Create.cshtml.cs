using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar.Kullanicilar;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    public CreateModel(AppDbContext context) => _context = context;

    public Firma? Firma { get; set; }

    [BindProperty] public int     FirmaID           { get; set; }
    [BindProperty] public string  KullaniciAdi      { get; set; } = "";
    [BindProperty] public string  Username          { get; set; } = "";
    [BindProperty] public string  Password          { get; set; } = "";
    [BindProperty] public bool    IsActive          { get; set; } = true;
    [BindProperty] public bool    IsFirmaAdmin      { get; set; } = false;
    [BindProperty] public bool    IsSistemAdmin     { get; set; } = false;
    [BindProperty] public byte    SiparisYetkisi    { get; set; } = 0;
    [BindProperty] public byte    ForwardingYetkisi { get; set; } = 0;
    [BindProperty] public byte    AracYetkisi       { get; set; } = 0;
    [BindProperty] public byte    SeferYetkisi      { get; set; } = 0;
    [BindProperty] public byte    MusteriYetkisi    { get; set; } = 0;
    [BindProperty] public byte    CariYetkisi       { get; set; } = 0;
    [BindProperty] public byte    RaporYetkisi      { get; set; } = 0;

    public string? Hata { get; set; }

    public async Task<IActionResult> OnGetAsync(int firmaId)
    {
        Firma = await _context.Firmalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == firmaId);
        if (Firma == null) return NotFound();

        FirmaID = firmaId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ── Kanonik Firma kaydını bul (Kullanicilar.FirmaID buna bağlı) ───
        Firma = await _context.Firmalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == FirmaID);
        if (Firma == null) return NotFound();

        var operasyonFirma = Firma;   // kanonik tablo = operasyon FK hedefi

        // ── Zorunlu alan kontrolü ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(KullaniciAdi) ||
            string.IsNullOrWhiteSpace(Username)     ||
            string.IsNullOrWhiteSpace(Password))
        {
            Hata = "Ad Soyad, Kullanıcı Adı ve Şifre zorunludur.";
            return Page();
        }

        // ── Username benzersizlik (aynı Firmalar.FirmaID içinde) ──────────
        bool usernameVarMi = await _context.Kullanicilar
            .AnyAsync(k => k.FirmaID == operasyonFirma.FirmaID
                        && k.Username == Username.Trim());
        if (usernameVarMi)
        {
            Hata = $"'{Username}' kullanıcı adı bu firmada zaten kullanılıyor.";
            return Page();
        }

        // ── Aktif kullanıcı limit kontrolü ───────────────────────────────
        // Limit kanonik Firma'dan alınır; sayım Firmalar.FirmaID üzerinden yapılır.
        // IsSistemAdmin kullanıcılar limite dahil değildir.
        if (IsActive)
        {
            int aktifSayi = await _context.Kullanicilar
                .CountAsync(k => k.FirmaID == operasyonFirma.FirmaID
                              && k.IsActive
                              && !k.IsSistemAdmin);
            if (aktifSayi >= (Firma.KullaniciLimiti ?? 0))
            {
                Hata = $"Kullanıcı limiti dolu ({Firma.KullaniciLimiti} aktif kullanıcı). " +
                       "Yeni aktif kullanıcı eklenemez. Pasif olarak ekleyebilirsiniz.";
                return Page();
            }
        }

        // ── Kullanici kaydını oluştur — FirmaID = Firmalar.FirmaID ────────
        var kullanici = new Kullanici
        {
            FirmaID           = operasyonFirma.FirmaID,   // ← Firmalar tablosunun ID'si
            KullaniciAdi      = KullaniciAdi.Trim(),
            Username          = Username.Trim(),
            Password          = Password,
            IsActive          = IsActive,
            IsFirmaAdmin      = IsFirmaAdmin,
            IsSistemAdmin     = IsSistemAdmin,
            YetkiSeviyesi1    = 0,   // DB kolonu NOT NULL; model int? ama DB default yok
            YetkiSeviyesi2    = 0,
            SiparisYetkisi    = SiparisYetkisi,
            ForwardingYetkisi = ForwardingYetkisi,
            AracYetkisi       = AracYetkisi,
            SeferYetkisi      = SeferYetkisi,
            MusteriYetkisi    = MusteriYetkisi,
            CariYetkisi       = CariYetkisi,
            RaporYetkisi      = RaporYetkisi,
            CreatedAt         = DateTime.Now,
        };

        _context.Kullanicilar.Add(kullanici);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index", new { firmaId = FirmaID });
    }
}
