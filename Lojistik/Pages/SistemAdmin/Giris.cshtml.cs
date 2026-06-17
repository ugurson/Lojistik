using Lojistik.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lojistik.Pages.SistemAdmin;

public class GirisModel : PageModel
{
    private readonly AppDbContext _context;

    public GirisModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string? Hata { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Zaten giriş yapmışsa dashboard'a yönlendir
        var result = await HttpContext.AuthenticateAsync("SistemAdminScheme");
        if (result.Succeeded)
            return RedirectToPage("/SistemAdmin/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Hata = "Kullanıcı adı ve şifre zorunludur.";
            return Page();
        }

        // Kullanıcıyı bul: Username + Password + IsActive + IsSistemAdmin
        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .Where(k => k.Username == Username
                     && k.Password == Password
                     && k.IsActive
                     && k.IsSistemAdmin)
            .Select(k => new
            {
                k.KullaniciID,
                k.Username,
                k.KullaniciAdi
            })
            .FirstOrDefaultAsync();

        if (kullanici == null)
        {
            Hata = "Kullanıcı adı, şifre veya yetki hatalı.";
            return Page();
        }

        // Claim'leri oluştur
        var claims = new List<Claim>
        {
            new("KullaniciID",  kullanici.KullaniciID.ToString()),
            new("Username",     kullanici.Username),
            new("KullaniciAdi", kullanici.KullaniciAdi),
            new("IsSistemAdmin","true"),
        };

        var identity  = new ClaimsIdentity(claims, "SistemAdminScheme");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("SistemAdminScheme", principal, new AuthenticationProperties
        {
            IsPersistent = false   // Tarayıcı kapanınca oturum biter
        });

        return RedirectToPage("/SistemAdmin/Index");
    }
}
