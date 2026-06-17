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
        var f = await _context.ProgramFirmalar.AsNoTracking()
            .FirstOrDefaultAsync(x => x.FirmaID == id);
        if (f == null) return NotFound();

        FirmaID         = f.FirmaID;
        FirmaKodu       = f.FirmaKodu;
        FirmaAdi        = f.FirmaAdi;
        PaketAdi        = f.PaketAdi;
        KullaniciLimiti = f.KullaniciLimiti;
        AylikUcret      = f.AylikUcret;
        ParaBirimi      = f.ParaBirimi;
        BaslamaTarihi   = f.BaslamaTarihi;
        BitisTarihi     = f.BitisTarihi;
        IsActive        = f.IsActive;
        DemoMu          = f.DemoMu;
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

        // ── Benzersizlik: ProgramFirmalar ─────────────────────────────────
        if (await _context.ProgramFirmalar.AnyAsync(
                f => f.FirmaKodu == kodTemiz && f.FirmaID != FirmaID))
        {
            Hata = $"'{kodTemiz}' firma kodu başka bir program firması tarafından kullanılıyor.";
            CreatedAt = await GetCreatedAtAsync();
            return Page();
        }

        // ── ProgramFirma kaydını yükle; eski FirmaKodu'nu sakla ───────────
        var programFirma = await _context.ProgramFirmalar.FindAsync(FirmaID);
        if (programFirma == null) return NotFound();

        var eskiKod = programFirma.FirmaKodu;   // Firmalar tablosunu bulmak için kullanılacak

        // ── Benzersizlik: Firmalar ────────────────────────────────────────
        // Yeni kod başka bir Firmalar kaydında varsa ve o kayıt "bizim" kaydımız değilse engelle.
        if (await _context.Firmalar.AnyAsync(
                f => f.FirmaKodu == kodTemiz && f.FirmaKodu != eskiKod))
        {
            Hata = $"'{kodTemiz}' firma kodu operasyon tablosunda başka bir firma tarafından kullanılıyor.";
            CreatedAt = await GetCreatedAtAsync();
            return Page();
        }

        // ── Operasyon tablosundaki karşılık gelen Firma kaydını bul ───────
        var operasyonFirma = await _context.Firmalar
            .FirstOrDefaultAsync(f => f.FirmaKodu == eskiKod);

        if (operasyonFirma == null)
        {
            // Karşılık gelen Firmalar kaydı yok (senkron dışı veri).
            // Sistem bozulmasın: yeni bir Firmalar kaydı oluştur.
            operasyonFirma = new Firma
            {
                FirmaKodu = kodTemiz,
                FirmaAdi  = FirmaAdi.Trim(),
                IsActive  = IsActive,
                CreatedAt = DateTime.Now
            };
            _context.Firmalar.Add(operasyonFirma);
        }
        else
        {
            // Mevcut Firmalar kaydını güncelle
            operasyonFirma.FirmaKodu  = kodTemiz;
            operasyonFirma.FirmaAdi   = FirmaAdi.Trim();
            operasyonFirma.IsActive   = IsActive;
            operasyonFirma.UpdatedAt  = DateTime.Now;
        }

        // ── Limit düşürme uyarısı ─────────────────────────────────────────
        // Kullanicilar.FirmaID → Firmalar.FirmaID; doğru ID ile count alınır.
        // operasyonFirma henüz DB'ye yazılmadıysa (yeni kayıt) FirmaID=0 olur;
        // bu durumda kullanıcı zaten yoktur, uyarı gereksizdir.
        if (operasyonFirma.FirmaID > 0)
        {
            int aktifSayi = await _context.Kullanicilar
                .CountAsync(k => k.FirmaID == operasyonFirma.FirmaID
                              && k.IsActive
                              && !k.IsSistemAdmin);
            if (KullaniciLimiti < aktifSayi)
            {
                Uyari = $"Yeni limit ({KullaniciLimiti}), mevcut aktif kullanıcı sayısının ({aktifSayi}) " +
                        "altında. Kayıt yapıldı, ancak yeni kullanıcı eklenemez.";
            }
        }

        // ── ProgramFirma alanlarını güncelle ──────────────────────────────
        programFirma.FirmaKodu       = kodTemiz;
        programFirma.FirmaAdi        = FirmaAdi.Trim();
        programFirma.PaketAdi        = PaketAdi?.Trim();
        programFirma.KullaniciLimiti = KullaniciLimiti;
        programFirma.AylikUcret      = AylikUcret;
        programFirma.ParaBirimi      = ParaBirimi.Trim();
        programFirma.BaslamaTarihi   = BaslamaTarihi;
        programFirma.BitisTarihi     = BitisTarihi;
        programFirma.IsActive        = IsActive;
        programFirma.DemoMu          = DemoMu;
        programFirma.DemoKayitLimiti = DemoMu ? DemoKayitLimiti : null;
        programFirma.Notlar          = Notlar?.Trim();
        programFirma.UpdatedAt       = DateTime.Now;

        // EF Core: ProgramFirmalar + Firmalar değişiklikleri tek SaveChanges = tek transaction
        await _context.SaveChangesAsync();

        if (Uyari != null)
        {
            CreatedAt = programFirma.CreatedAt;
            return Page();
        }

        return RedirectToPage("Details", new { id = FirmaID });
    }

    // ── Yardımcı: hata durumunda CreatedAt'ı DB'den çek ──────────────────
    private async Task<DateTime?> GetCreatedAtAsync() =>
        await _context.ProgramFirmalar.AsNoTracking()
            .Where(f => f.FirmaID == FirmaID)
            .Select(f => (DateTime?)f.CreatedAt)
            .FirstOrDefaultAsync();
}
