using Lojistik.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);


var culture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient<ICurrencyRateService, CurrencyRateService>();


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
