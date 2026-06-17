using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.FirmaPanel.Kullanicilar;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    public CreateModel(AppDbContext context) => _context = context;

    [BindProperty] public string KullaniciAdi      { get; set; } = "";
    [BindProperty] public string Username          { get; set; } = "";
    [BindProperty] public string Password          { get; set; } = "";
    [BindProperty] public bool   IsActive          { get; set; } = true;
    [BindProperty] public bool   IsFirmaAdmin      { get; set; } = false;
    [BindProperty] public byte   SiparisYetkisi    { get; set; } = 0;
    [BindProperty] public byte   ForwardingYetkisi { get; set; } = 0;
    [BindProperty] public byte   AracYetkisi       { get; set; } = 0;
    [BindProperty] public byte   SeferYetkisi      { get; set; } = 0;
    [BindProperty] public byte   MusteriYetkisi    { get; set; } = 0;
    [BindProperty] public byte   CariYetkisi       { get; set; } = 0;
    [BindProperty] public byte   RaporYetkisi      { get; set; } = 0;

    public string? Hata            { get; set; }
    public int     KullaniciLimiti { get; set; }
    public int     AktifSayisi     { get; set; }

    private int? ClaimFirmaId() =>
        int.TryParse(User.FindFirst("FirmaID")?.Value, out var id) ? id : null;

    // ── GET ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync()
    {
        var firmaId = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        await YukleLimitAsync(firmaId.Value);
        return Page();
    }

    // ── POST ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        var firmaId = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        await YukleLimitAsync(firmaId.Value);

        // Zorunlu alan
        if (string.IsNullOrWhiteSpace(KullaniciAdi) ||
            string.IsNullOrWhiteSpace(Username)     ||
            string.IsNullOrWhiteSpace(Password))
        {
            Hata = "Ad Soyad, Kullanıcı Adı ve Şifre zorunludur.";
            return Page();
        }

        // Username benzersizlik (aynı firma içinde)
        bool usernameVarMi = await _context.Kullanicilar
            .AnyAsync(k => k.FirmaID == firmaId.Value
                        && k.Username == Username.Trim());
        if (usernameVarMi)
        {
            Hata = $"'{Username}' kullanıcı adı bu firmada zaten kullanılıyor.";
            return Page();
        }

        // Aktif kullanıcı limiti kontrolü
        if (IsActive && AktifSayisi >= KullaniciLimiti)
        {
            Hata = "Bu firmanın kullanıcı limiti dolmuştur. Yeni aktif kullanıcı oluşturulamaz.";
            return Page();
        }

        var kullanici = new Kullanici
        {
            FirmaID           = firmaId.Value,
            KullaniciAdi      = KullaniciAdi.Trim(),
            Username          = Username.Trim(),
            Password          = Password,           // mevcut mimari: düz metin
            IsActive          = IsActive,
            IsFirmaAdmin      = IsFirmaAdmin,
            IsSistemAdmin     = false,              // firma admin asla sistem admin yapamaz
            YetkiSeviyesi1    = 0,                  // DB NOT NULL; varsayılan 0
            YetkiSeviyesi2    = 0,
            SiparisYetkisi    = SiparisYetkisi,
            ForwardingYetkisi = ForwardingYetkisi,
            AracYetkisi       = AracYetkisi,
            SeferYetkisi      = SeferYetkisi,
            MusteriYetkisi    = MusteriYetkisi,
            CariYetkisi       = CariYetkisi,
            RaporYetkisi      = RaporYetkisi,
            // SubeKodu ve AltSubeKodu null bırakılır — yeni kullanıcı tüm veriyi görebilir.
            // İleri aşamada şube bazlı filtreleme gerekirse düzenlenebilir.
            CreatedAt         = DateTime.Now,
        };

        _context.Kullanicilar.Add(kullanici);
        await _context.SaveChangesAsync();

        TempData["Basari"] = $"'{kullanici.KullaniciAdi}' kullanıcısı başarıyla oluşturuldu.";
        return RedirectToPage("Index");
    }

    // ProgramFirmalar.KullaniciLimiti + aktif kullanıcı sayısı
    private async Task YukleLimitAsync(int firmaId)
    {
        var firmaKodu = await _context.Firmalar.AsNoTracking()
            .Where(f => f.FirmaID == firmaId)
            .Select(f => f.FirmaKodu)
            .FirstOrDefaultAsync();

        if (firmaKodu != null)
        {
            KullaniciLimiti = await _context.ProgramFirmalar.AsNoTracking()
                .Where(p => p.FirmaKodu == firmaKodu)
                .Select(p => p.KullaniciLimiti)
                .FirstOrDefaultAsync();
        }

        AktifSayisi = await _context.Kullanicilar
            .CountAsync(k => k.FirmaID  == firmaId
                          && k.IsActive
                          && !k.IsSistemAdmin);
    }
}
