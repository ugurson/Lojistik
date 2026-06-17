using Lojistik.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lojistik.Pages.FirmaPanel.Kullanicilar;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    public EditModel(AppDbContext context) => _context = context;

    [BindProperty] public int    KullaniciID      { get; set; }
    [BindProperty] public string KullaniciAdi     { get; set; } = "";
    [BindProperty] public string Username         { get; set; } = "";
    [BindProperty] public string Password         { get; set; } = "";   // boş = değiştirme
    [BindProperty] public bool   IsActive         { get; set; }
    [BindProperty] public bool   IsFirmaAdmin     { get; set; }
    [BindProperty] public byte   SiparisYetkisi   { get; set; }
    [BindProperty] public byte   ForwardingYetkisi{ get; set; }
    [BindProperty] public byte   AracYetkisi      { get; set; }
    [BindProperty] public byte   SeferYetkisi     { get; set; }
    [BindProperty] public byte   MusteriYetkisi   { get; set; }
    [BindProperty] public byte   CariYetkisi      { get; set; }
    [BindProperty] public byte   RaporYetkisi     { get; set; }

    public string?   Hata      { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int       KullaniciLimiti { get; set; }
    public int       AktifSayisi    { get; set; }

    private int? ClaimFirmaId() =>
        int.TryParse(User.FindFirst("FirmaID")?.Value, out var id) ? id : null;

    private int ClaimKullaniciId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    // ── GET ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var firmaId = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        var k = await _context.Kullanicilar.AsNoTracking()
            .FirstOrDefaultAsync(x => x.KullaniciID == id
                                   && x.FirmaID     == firmaId.Value);
        if (k == null)     return NotFound();
        if (k.IsSistemAdmin) return NotFound(); // sistem admin düzenlenemez

        KullaniciID       = k.KullaniciID;
        KullaniciAdi      = k.KullaniciAdi ?? "";
        Username          = k.Username;
        Password          = "";                   // şifre formu boş başlar
        IsActive          = k.IsActive;
        IsFirmaAdmin      = k.IsFirmaAdmin;
        SiparisYetkisi    = k.SiparisYetkisi;
        ForwardingYetkisi = k.ForwardingYetkisi;
        AracYetkisi       = k.AracYetkisi;
        SeferYetkisi      = k.SeferYetkisi;
        MusteriYetkisi    = k.MusteriYetkisi;
        CariYetkisi       = k.CariYetkisi;
        RaporYetkisi      = k.RaporYetkisi;
        CreatedAt         = k.CreatedAt;

        await YukleLimitAsync(firmaId.Value);
        return Page();
    }

    // ── POST ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        var firmaId = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        // Kullanıcıyı yükle ve firma + sistem admin doğrula
        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.KullaniciID == KullaniciID
                                   && x.FirmaID     == firmaId.Value);
        if (kullanici == null)      return NotFound();
        if (kullanici.IsSistemAdmin) return NotFound();

        await YukleLimitAsync(firmaId.Value);
        CreatedAt = kullanici.CreatedAt;

        // ── Zorunlu alan ──────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(KullaniciAdi) ||
            string.IsNullOrWhiteSpace(Username))
        {
            Hata = "Ad Soyad ve Kullanıcı Adı zorunludur.";
            return Page();
        }

        // ── Username benzersizlik ─────────────────────────────────────────
        bool usernameVarMi = await _context.Kullanicilar
            .AnyAsync(k => k.FirmaID     == firmaId.Value
                        && k.Username    == Username.Trim()
                        && k.KullaniciID != KullaniciID);
        if (usernameVarMi)
        {
            Hata = $"'{Username}' kullanıcı adı bu firmada zaten kullanılıyor.";
            return Page();
        }

        // ── Kendi kendini koruma ──────────────────────────────────────────
        int loginId = ClaimKullaniciId();
        bool editingSelf = (loginId == KullaniciID);
        if (editingSelf)
        {
            bool isActiveKaldirildi  = kullanici.IsActive    && !IsActive;
            bool adminYetkisiAlindi  = kullanici.IsFirmaAdmin && !IsFirmaAdmin;
            if (isActiveKaldirildi || adminYetkisiAlindi)
            {
                Hata = "Kendi yönetici yetkinizi veya aktiflik durumunuzu değiştiremezsiniz.";
                return Page();
            }
        }

        // ── Son aktif firma admin koruması ────────────────────────────────
        // Bu kullanıcı aktif+admin iken pasif yapılıyor VEYA admin yetkisi kaldırılıyorsa kontrol et
        bool adminStatusKalkiyor = (kullanici.IsActive && kullanici.IsFirmaAdmin)
                                && (!IsActive || !IsFirmaAdmin);
        if (adminStatusKalkiyor)
        {
            int digerAktifAdmin = await _context.Kullanicilar
                .CountAsync(x => x.FirmaID     == firmaId.Value
                              && x.IsActive
                              && x.IsFirmaAdmin
                              && !x.IsSistemAdmin
                              && x.KullaniciID != KullaniciID);
            if (digerAktifAdmin == 0)
            {
                Hata = "Firmada en az bir aktif firma yöneticisi kalmalıdır.";
                return Page();
            }
        }

        // ── Pasif → Aktif geçişinde limit kontrolü ────────────────────────
        if (!kullanici.IsActive && IsActive)
        {
            if (AktifSayisi >= KullaniciLimiti)
            {
                Hata = "Bu firmanın kullanıcı limiti dolmuştur. Yeni aktif kullanıcı oluşturulamaz.";
                return Page();
            }
        }

        // ── Kullanıcıyı güncelle ─────────────────────────────────────────
        kullanici.KullaniciAdi      = KullaniciAdi.Trim();
        kullanici.Username          = Username.Trim();
        kullanici.IsActive          = IsActive;
        kullanici.IsFirmaAdmin      = IsFirmaAdmin;
        kullanici.SiparisYetkisi    = SiparisYetkisi;
        kullanici.ForwardingYetkisi = ForwardingYetkisi;
        kullanici.AracYetkisi       = AracYetkisi;
        kullanici.SeferYetkisi      = SeferYetkisi;
        kullanici.MusteriYetkisi    = MusteriYetkisi;
        kullanici.CariYetkisi       = CariYetkisi;
        kullanici.RaporYetkisi      = RaporYetkisi;
        kullanici.UpdatedAt         = DateTime.Now;

        // Şifre: boş bırakılmışsa değiştirme
        if (!string.IsNullOrWhiteSpace(Password))
            kullanici.Password = Password;

        await _context.SaveChangesAsync();

        TempData["Basari"] = $"'{kullanici.KullaniciAdi}' kullanıcısı güncellendi.";
        return RedirectToPage("Index");
    }

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
