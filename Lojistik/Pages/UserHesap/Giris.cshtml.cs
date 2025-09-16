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

        // 1) Firma kodu var mı?
        var firma = await _db.Firmalar.AsNoTracking()
                       .FirstOrDefaultAsync(f => f.FirmaKodu == Input.FirmaKodu);
        if (firma is null)
        {
            ModelState.AddModelError(nameof(Input.FirmaKodu), "Firma kodu hatalı.");
            return Page();
        }


        // 2) Kullanıcı + şifre + firma eşleşmeli (firma kodu OPSİYONEL DEĞİL)
        var user = await _db.Kullanicilar.AsNoTracking()
                       .Where(k => k.FirmaID == firma.FirmaID
                                && k.Username == Input.Username
                                && k.Password == Input.Password)
                       .Select(k => new { k.KullaniciID, k.Username, k.FirmaID })
                       .FirstOrDefaultAsync();

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return Page();
        }

        // 3) Claims
        var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.KullaniciID.ToString()),
        new(System.Security.Claims.ClaimTypes.Name, user.Username),
        new("FirmaID",  user.FirmaID.ToString()),
        new("FirmaKodu", firma.FirmaKodu)
    };

        // FirmaKodu verilmişse ona göre filtrele
        var q =
            from k in _db.Kullanicilar
            join f in _db.Firmalar on k.FirmaID equals f.FirmaID
            where k.Username == Input.Username && k.Password == Input.Password
            && (string.IsNullOrEmpty(Input.FirmaKodu) || f.FirmaKodu == Input.FirmaKodu)
            select new
            {
                k.KullaniciID,
                k.Username,
                k.FirmaID,
                f.FirmaKodu
            };

        var u = await q.FirstOrDefaultAsync();
        if (u == null)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı/şifre veya firma kodu hatalı.");
            return Page();
        }


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
