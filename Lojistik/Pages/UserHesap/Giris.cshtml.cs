using System.Security.Claims;
using Lojistik.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.UserHesap;

public class GirisModel : PageModel
{
    private readonly AppDbContext _db;
    public GirisModel(AppDbContext db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Firma kodu zorunludur.")]   // ← ZORUNLU
        public string FirmaKodu { get; set; } = "";
        public bool BeniHatirla { get; set; } = true;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        if (!ModelState.IsValid) return Page();

        // Temizle/normalize
        Input.FirmaKodu = (Input.FirmaKodu ?? "").Trim();

        // 1) Firmalar tablosunda firma kodu var mı + aktif mi?
        //    Lisans/aktiflik artık kanonik Firmalar tablosundan okunur (eski 2. ProgramFirmalar
        //    sorgusu kaldırıldı). SistemAdmin panelinden firma pasife alınmışsa (Firmalar.IsActive=0)
        //    giriş aynı şekilde engellenir; tek satır hem firmayı hem aktiflik durumunu verir.
        var firma = await _db.Firmalar.AsNoTracking()
                       .FirstOrDefaultAsync(f => f.FirmaKodu == Input.FirmaKodu);
        if (firma is null || !firma.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı, şifre, firma kodu veya yetki durumu hatalı.");
            return Page();
        }

        // 3) Kullanıcı + şifre + firma eşleşmeli, kullanıcı aktif olmalı
        var user = await _db.Kullanicilar.AsNoTracking()
                       .Where(k => k.FirmaID == firma.FirmaID
                                && k.Username == Input.Username
                                && k.Password == Input.Password
                                && k.IsActive)
                       .Select(k => new {
                           k.KullaniciID,
                           k.Username,
                           k.FirmaID,
                           k.SubeKodu,
                           k.AltSubeKodu,
                           k.AracYetkisi,
                           k.SiparisYetkisi,
                           k.SeferYetkisi,
                           k.MusteriYetkisi,
                           k.CariYetkisi,
                           k.RaporYetkisi,
                           k.ForwardingYetkisi,
                           k.IsFirmaAdmin
                       })
                       .FirstOrDefaultAsync();

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı, şifre, firma kodu veya yetki durumu hatalı.");
            return Page();
        }

        // 3) Claims
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.KullaniciID.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.Username),
            new("FirmaID",           user.FirmaID.ToString()),
            new("FirmaKodu",         firma.FirmaKodu),
            new("AracYetkisi",       user.AracYetkisi.ToString()),
            new("SiparisYetkisi",    user.SiparisYetkisi.ToString()),
            new("SeferYetkisi",      user.SeferYetkisi.ToString()),
            new("MusteriYetkisi",    user.MusteriYetkisi.ToString()),
            new("CariYetkisi",       user.CariYetkisi.ToString()),
            new("RaporYetkisi",      user.RaporYetkisi.ToString()),
            new("ForwardingYetkisi", user.ForwardingYetkisi.ToString()),
        };

        // Firma admin ise claim ekle (FirmaPanel erişimi bu claim ile kontrol edilir)
        if (user.IsFirmaAdmin)
            claims.Add(new Claim("IsFirmaAdmin", "true"));

        if (!string.IsNullOrWhiteSpace(user.AltSubeKodu))
            claims.Add(new Claim("AltSubeKodu", user.AltSubeKodu.Trim()));

        if (!string.IsNullOrWhiteSpace(user.SubeKodu))
            claims.Add(new Claim("SubeKodu", user.SubeKodu.Trim()));


        var identity = new System.Security.Claims.ClaimsIdentity(
            claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = Input.BeniHatirla, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

        return LocalRedirect(ReturnUrl);
    }

}
