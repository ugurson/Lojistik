using Lojistik.Data;
using Lojistik.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
var builder = WebApplication.CreateBuilder(args);


var culture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient<ICurrencyRateService, CurrencyRateService>();
builder.Services.AddScoped<IEmailService, EmailService>();


builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    // ── Ana uygulama cookie’si ──────────────────────────────────────────
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath      = "/UserHesap/Giris";
        options.AccessDeniedPath = "/UserHesap/Yetkisiz";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name    = ".LojistikApp";

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                // SistemAdmin yolları → admin giriş sayfasına yönlendir
                if (ctx.Request.Path.StartsWithSegments("/SistemAdmin"))
                {
                    ctx.Response.Redirect("/SistemAdmin/Giris");
                    return Task.CompletedTask;
                }
                var uri = new Uri(ctx.RedirectUri);
                var q = QueryHelpers.ParseQuery(uri.Query);
                if (q.TryGetValue("ReturnUrl", out var ru) && (ru == "/" || ru == "%2F"))
                {
                    ctx.Response.Redirect("/UserHesap/Giris");
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    })
    // ── SistemAdmin ayrı cookie’si ──────────────────────────────────────
    .AddCookie("SistemAdminScheme", options =>
    {
        options.LoginPath        = "/SistemAdmin/Giris";
        options.AccessDeniedPath = "/SistemAdmin/Giris";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name      = ".LojistikSistemAdmin";

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                // Bu scheme yalnızca /SistemAdmin yolları için geçerlidir.
                // Başka bir path’ten yanlışlıkla challenge gelirse normal login’e yönlendir.
                if (ctx.Request.Path.StartsWithSegments("/SistemAdmin"))
                {
                    ctx.Response.Redirect("/SistemAdmin/Giris");
                }
                else
                {
                    ctx.Response.Redirect("/UserHesap/Giris");
                }
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization(options =>
{
    // ── Default policy: sadece ana uygulama scheme'i ────────────────────
    // SistemAdminScheme burada olmamalı; olursa / açılınca
    // /SistemAdmin/Giris'e yönlendirme riski doğar.
    options.DefaultPolicy = new AuthorizationPolicyBuilder(
            CookieAuthenticationDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();

    // ── SistemAdmin paneli ──────────────────────────────────────────────
    options.AddPolicy("SistemAdminPolicy", p => p
        .AddAuthenticationSchemes("SistemAdminScheme")
        .RequireAuthenticatedUser()
        .RequireClaim("IsSistemAdmin", "true"));

    // ── Firma admin paneli ───────────────────────────────────────────────
    options.AddPolicy("FirmaAdminPolicy", p => p
        .RequireAuthenticatedUser()
        .RequireClaim("IsFirmaAdmin", "true"));

    // Araç modülü
    options.AddPolicy("AracGorebilir",   p => p.RequireClaim("AracYetkisi",      "1", "2"));
    options.AddPolicy("AracTamYetki",    p => p.RequireClaim("AracYetkisi",      "1"));

    // Diğer modüller
    options.AddPolicy("SiparisGorebilir",    p => p.RequireClaim("SiparisYetkisi",    "1", "2"));
    options.AddPolicy("SeferGorebilir",      p => p.RequireClaim("SeferYetkisi",      "1", "2"));
    options.AddPolicy("MusteriGorebilir",    p => p.RequireClaim("MusteriYetkisi",    "1", "2"));
    options.AddPolicy("CariGorebilir",       p => p.RequireClaim("CariYetkisi",       "1", "2"));
    options.AddPolicy("RaporGorebilir",      p => p.RequireClaim("RaporYetkisi",      "1", "2"));
    options.AddPolicy("ForwardingGorebilir", p => p.RequireClaim("ForwardingYetkisi", "1", "2"));
});

// 🔒 Razor Pages kuralları (tek yerde)
builder.Services.AddRazorPages(options =>
{
    // Sadece bu sayfalar anonim
    options.Conventions.AllowAnonymousToPage("/UserHesap/Giris");
    options.Conventions.AllowAnonymousToPage("/UserHesap/Yetkisiz");

    // Tüm siteyi koru → /Index dahil hepsi login ister
    options.Conventions.AuthorizeFolder("/");

    // Modül bazlı klasör yetkileri
    options.Conventions.AuthorizeFolder("/Siparisler",      "SiparisGorebilir");
    options.Conventions.AuthorizeFolder("/Seferler",        "SeferGorebilir");
    options.Conventions.AuthorizeFolder("/SeferGelirleri",  "SeferGorebilir");
    options.Conventions.AuthorizeFolder("/SeferMasraflari", "SeferGorebilir");
    options.Conventions.AuthorizeFolder("/Musteriler",      "MusteriGorebilir");
    options.Conventions.AuthorizeFolder("/Cari",            "CariGorebilir");
    options.Conventions.AuthorizeFolder("/Raporlar",        "RaporGorebilir");
    options.Conventions.AuthorizeFolder("/Forwarding",      "ForwardingGorebilir");
    options.Conventions.AuthorizeFolder("/Araclar",         "AracGorebilir");
    options.Conventions.AuthorizeFolder("/Soforler",        "AracGorebilir");
    options.Conventions.AuthorizeFolder("/Belgeler",        "AracGorebilir");
    options.Conventions.AuthorizeFolder("/Kademeler",       "AracGorebilir");
    options.Conventions.AuthorizeFolder("/KademeFirmalari", "AracGorebilir");
    options.Conventions.AuthorizeFolder("/Sevkiyatlar",     "SeferGorebilir");
    options.Conventions.AuthorizeFolder("/SeferSevkiyatlar","SeferGorebilir");

    // ── SistemAdmin klasörü ─────────────────────────────────────────────
    options.Conventions.AuthorizeFolder("/SistemAdmin", "SistemAdminPolicy");
    options.Conventions.AllowAnonymousToPage("/SistemAdmin/Giris");

    // ── FirmaPanel klasörü ───────────────────────────────────────────────
    options.Conventions.AuthorizeFolder("/FirmaPanel", "FirmaAdminPolicy");
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
app.Use(async (ctx, next) =>
{
    // Login değilken "/" gelirse içeride login sayfasını çalıştır
    if (ctx.Request.Path == "/" &&
        !(ctx.User?.Identity?.IsAuthenticated ?? false))
    {
        ctx.Request.Path = "/UserHesap/Giris";
    }

    await next();
});


app.UseAuthorization();

app.MapRazorPages();
app.Run();
