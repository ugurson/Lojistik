using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    public CreateModel(AppDbContext context) => _context = context;

    [BindProperty] public string   FirmaKodu        { get; set; } = "";
    [BindProperty] public string   FirmaAdi         { get; set; } = "";
    [BindProperty] public string?  PaketAdi         { get; set; }
    [BindProperty] public int      KullaniciLimiti  { get; set; } = 5;
    [BindProperty] public decimal? AylikUcret       { get; set; }
    [BindProperty] public string   ParaBirimi       { get; set; } = "TL";
    [BindProperty] public DateOnly BaslamaTarihi    { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public DateOnly? BitisTarihi     { get; set; }
    [BindProperty] public bool     IsActive         { get; set; } = true;
    [BindProperty] public bool     DemoMu           { get; set; } = false;
    [BindProperty] public int?     DemoKayitLimiti  { get; set; }
    [BindProperty] public string?  Notlar           { get; set; }

    public string? Hata { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(FirmaKodu) || string.IsNullOrWhiteSpace(FirmaAdi))
        {
            Hata = "Firma Kodu ve Firma Adı zorunludur.";
            return Page();
        }

        if (KullaniciLimiti < 1)
        {
            Hata = "Kullanıcı limiti en az 1 olmalıdır.";
            return Page();
        }

        var kodTemiz = FirmaKodu.Trim().ToUpper();

        // ── Benzersizlik: her iki tablo da kontrol edilir ─────────────────
        if (await _context.ProgramFirmalar.AnyAsync(f => f.FirmaKodu == kodTemiz))
        {
            Hata = $"'{kodTemiz}' firma kodu zaten kullanılıyor.";
            return Page();
        }
        if (await _context.Firmalar.AnyAsync(f => f.FirmaKodu == kodTemiz))
        {
            Hata = $"'{kodTemiz}' firma kodu operasyon tablosunda zaten kullanılıyor.";
            return Page();
        }

        var now = DateTime.Now;

        // ── SaaS / lisans tablosu ─────────────────────────────────────────
        var programFirma = new ProgramFirma
        {
            FirmaKodu       = kodTemiz,
            FirmaAdi        = FirmaAdi.Trim(),
            PaketAdi        = PaketAdi?.Trim(),
            KullaniciLimiti = KullaniciLimiti,
            AylikUcret      = AylikUcret,
            ParaBirimi      = ParaBirimi.Trim(),
            BaslamaTarihi   = BaslamaTarihi,
            BitisTarihi     = BitisTarihi,
            IsActive        = IsActive,
            DemoMu          = DemoMu,
            DemoKayitLimiti = DemoMu ? DemoKayitLimiti : null,
            Notlar          = Notlar?.Trim(),
            CreatedAt       = now
        };

        // ── Operasyon tablosu (Kullanicilar.FirmaID bu tabloya bağlı) ─────
        // Not: Firma modeli Telefon/Adres alanı içermiyor; mevcut alanlar senkronize edilir.
        var operasyonFirma = new Firma
        {
            FirmaKodu = kodTemiz,
            FirmaAdi  = FirmaAdi.Trim(),
            IsActive  = IsActive,
            CreatedAt = now
        };

        // EF Core tek SaveChangesAsync çağrısını otomatik olarak tek transaction'a sarar.
        // ProgramFirmalar veya Firmalar'dan biri başarısız olursa her ikisi de geri alınır.
        _context.ProgramFirmalar.Add(programFirma);
        _context.Firmalar.Add(operasyonFirma);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
