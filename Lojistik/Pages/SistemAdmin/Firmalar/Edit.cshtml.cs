using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    public EditModel(AppDbContext context) => _context = context;

    [BindProperty] public int      FirmaID         { get; set; }
    [BindProperty] public string   FirmaKodu       { get; set; } = "";
    [BindProperty] public string   FirmaAdi        { get; set; } = "";
    [BindProperty] public string?  PaketAdi        { get; set; }
    [BindProperty] public int      KullaniciLimiti { get; set; }
    [BindProperty] public decimal? AylikUcret      { get; set; }
    [BindProperty] public string   ParaBirimi      { get; set; } = "TL";
    [BindProperty] public DateOnly BaslamaTarihi   { get; set; }
    [BindProperty] public DateOnly? BitisTarihi    { get; set; }
    [BindProperty] public bool     IsActive        { get; set; }
    [BindProperty] public bool     DemoMu          { get; set; }
    [BindProperty] public int?     DemoKayitLimiti { get; set; }
    [BindProperty] public string?  Notlar          { get; set; }

    public string?   Hata      { get; set; }
    public string?   Uyari     { get; set; }   // limit düşürme uyarısı (kaydetmeyi engellemez)
    public DateTime? CreatedAt { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var f = await _context.Firmalar.AsNoTracking()
            .FirstOrDefaultAsync(x => x.FirmaID == id);
        if (f == null) return NotFound();

        FirmaID         = f.FirmaID;
        FirmaKodu       = f.FirmaKodu;
        FirmaAdi        = f.FirmaAdi;
        PaketAdi        = f.PaketAdi;
        KullaniciLimiti = f.KullaniciLimiti ?? 0;
        AylikUcret      = f.AylikUcret;
        ParaBirimi      = f.ParaBirimi ?? "TL";
        BaslamaTarihi   = f.BaslamaTarihi ?? DateOnly.FromDateTime(DateTime.Today);
        BitisTarihi     = f.BitisTarihi;
        IsActive        = f.IsActive;
        DemoMu          = f.DemoMu ?? false;
        DemoKayitLimiti = f.DemoKayitLimiti;
        Notlar          = f.Notlar;
        CreatedAt       = f.CreatedAt;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ── Zorunlu alan kontrolü ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(FirmaKodu) || string.IsNullOrWhiteSpace(FirmaAdi))
        {
            Hata = "Firma Kodu ve Firma Adı zorunludur.";
            CreatedAt = await GetCreatedAtAsync();
            return Page();
        }

        if (KullaniciLimiti < 1)
        {
            Hata = "Kullanıcı limiti en az 1 olmalıdır.";
            CreatedAt = await GetCreatedAtAsync();
            return Page();
        }

        var kodTemiz = FirmaKodu.Trim().ToUpper();

        // ── Kanonik Firma kaydını yükle ───────────────────────────────────
        var firma = await _context.Firmalar.FindAsync(FirmaID);
        if (firma == null) return NotFound();

        // ── Benzersizlik: kendisi hariç başka Firmalar kaydında kod var mı ─
        if (await _context.Firmalar.AnyAsync(
                f => f.FirmaKodu == kodTemiz && f.FirmaID != FirmaID))
        {
            Hata = $"'{kodTemiz}' firma kodu başka bir firma tarafından kullanılıyor.";
            CreatedAt = await GetCreatedAtAsync();
            return Page();
        }

        // ── Limit düşürme uyarısı (kaydetmeyi engellemez) ─────────────────
        // Kullanicilar.FirmaID → Firmalar.FirmaID; doğru ID ile count alınır.
        int aktifSayi = await _context.Kullanicilar
            .CountAsync(k => k.FirmaID == firma.FirmaID
                          && k.IsActive
                          && !k.IsSistemAdmin);
        if (KullaniciLimiti < aktifSayi)
        {
            Uyari = $"Yeni limit ({KullaniciLimiti}), mevcut aktif kullanıcı sayısının ({aktifSayi}) " +
                    "altında. Kayıt yapıldı, ancak yeni kullanıcı eklenemez.";
        }

        // ── Tek kanonik Firma kaydını güncelle (lisans alanları dahil) ────
        firma.FirmaKodu       = kodTemiz;
        firma.FirmaAdi        = FirmaAdi.Trim();
        firma.PaketAdi        = PaketAdi?.Trim();
        firma.KullaniciLimiti = KullaniciLimiti;
        firma.AylikUcret      = AylikUcret;
        firma.ParaBirimi      = ParaBirimi.Trim();
        firma.BaslamaTarihi   = BaslamaTarihi;
        firma.BitisTarihi     = BitisTarihi;
        firma.IsActive        = IsActive;
        firma.DemoMu          = DemoMu;
        firma.DemoKayitLimiti = DemoMu ? DemoKayitLimiti : null;
        firma.Notlar          = Notlar?.Trim();
        firma.UpdatedAt       = DateTime.Now;

        await _context.SaveChangesAsync();

        if (Uyari != null)
        {
            CreatedAt = firma.CreatedAt;
            return Page();
        }

        return RedirectToPage("Details", new { id = FirmaID });
    }

    // ── Yardımcı: hata durumunda CreatedAt'ı DB'den çek ──────────────────
    private async Task<DateTime?> GetCreatedAtAsync() =>
        await _context.Firmalar.AsNoTracking()
            .Where(f => f.FirmaID == FirmaID)
            .Select(f => (DateTime?)f.CreatedAt)
            .FirstOrDefaultAsync();
}
