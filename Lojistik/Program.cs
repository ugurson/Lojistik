using Lojistik.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Cookie Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/UserHesap/Giris";
        options.AccessDeniedPath = "/UserHesap/Yetkisiz";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// 🔒 Razor Pages kuralları (tek yerde)
builder.Services.AddRazorPages(options =>
{
    // Sadece bu sayfalar anonim
    options.Conventions.AllowAnonymousToPage("/UserHesap/Giris");
    options.Conventions.AllowAnonymousToPage("/UserHesap/Yetkisiz");

    // Tüm siteyi koru → /Index dahil hepsi login ister
    options.Conventions.AuthorizeFolder("/");

    // İstersen şunları tekrar serbest bırakabilirsin:
    // options.Conventions.AllowAnonymousToPage("/Privacy");
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// TR yerelleştirme
var supportedCultures = new[] { new CultureInfo("tr-TR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Sıra önemli
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();
