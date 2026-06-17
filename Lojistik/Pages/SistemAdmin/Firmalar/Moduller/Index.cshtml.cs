using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.Firmalar.Moduller;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    public IndexModel(AppDbContext context) => _context = context;

    public ProgramFirma? Firma { get; set; }

    // Tüm sistem modülleri + firma bazlı durum
    public record ModulSatir(
        int      ModulID,
        string   ModulKodu,
        string   ModulAdi,
        string?  Aciklama,
        int      SiraNo,
        bool     FirmaAktif,          // firmaya atanmış ve aktif mi?
        bool     AtanmisVar,          // ProgramFirmaModulleri kaydı var mı?
        int?     FirmaModulID,
        DateOnly? BaslamaTarihi,
        DateOnly? BitisTarihi
    );

    public List<ModulSatir> Moduller { get; set; } = new();

    // POST için binding
    [BindProperty] public int   FirmaID    { get; set; }
    [BindProperty] public List<ModulForm> ModulForms { get; set; } = new();

    public class ModulForm
    {
        public int    ModulID    { get; set; }
        public bool   IsActive   { get; set; }
    }

    public string? Basari { get; set; }

    public async Task<IActionResult> OnGetAsync(int firmaId)
    {
        Firma = await _context.ProgramFirmalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == firmaId);
        if (Firma == null) return NotFound();

        FirmaID = firmaId;
        await YukleModullerAsync(firmaId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Firma = await _context.ProgramFirmalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == FirmaID);
        if (Firma == null) return NotFound();

        // Tüm sistem modüllerini al
        var tumModuller = await _context.ProgramModulleri
            .AsNoTracking()
            .Where(m => m.IsActive)
            .ToListAsync();

        // Firmanın mevcut modül kayıtları
        var mevcutlar = await _context.ProgramFirmaModulleri
            .Where(x => x.FirmaID == FirmaID)
            .ToListAsync();

        foreach (var modul in tumModuller)
        {
            var form = ModulForms.FirstOrDefault(f => f.ModulID == modul.ModulID);
            bool yeniAktif = form?.IsActive ?? false;

            var mevcut = mevcutlar.FirstOrDefault(x => x.ModulID == modul.ModulID);

            if (mevcut == null)
            {
                // Kayıt yok — sadece aktif seçildiyse ekle
                if (yeniAktif)
                {
                    _context.ProgramFirmaModulleri.Add(new ProgramFirmaModul
                    {
                        FirmaID       = FirmaID,
                        ModulID       = modul.ModulID,
                        IsActive      = true,
                        BaslamaTarihi = DateOnly.FromDateTime(DateTime.Today),
                        CreatedAt     = DateTime.Now
                    });
                }
            }
            else
            {
                // Kayıt var — IsActive güncelle
                mevcut.IsActive = yeniAktif;
            }
        }

        await _context.SaveChangesAsync();

        Basari = "Modül ayarları kaydedildi.";
        await YukleModullerAsync(FirmaID);
        return Page();
    }

    private async Task YukleModullerAsync(int firmaId)
    {
        var tumModuller = await _context.ProgramModulleri
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SiraNo).ThenBy(m => m.ModulAdi)
            .ToListAsync();

        var firmaModuller = await _context.ProgramFirmaModulleri
            .AsNoTracking()
            .Where(x => x.FirmaID == firmaId)
            .ToListAsync();

        Moduller = tumModuller.Select(m =>
        {
            var fm = firmaModuller.FirstOrDefault(x => x.ModulID == m.ModulID);
            return new ModulSatir(
                m.ModulID,
                m.ModulKodu,
                m.ModulAdi,
                m.Aciklama,
                m.SiraNo,
                FirmaAktif:  fm?.IsActive ?? false,
                AtanmisVar:  fm != null,
                FirmaModulID: fm?.FirmaModulID,
                BaslamaTarihi: fm?.BaslamaTarihi,
                BitisTarihi:   fm?.BitisTarihi
            );
        }).ToList();
    }
}
