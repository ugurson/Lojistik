using System.Security.Claims;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Musteriler;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    public CreateModel(AppDbContext context) => _context = context;

    [BindProperty]
    public Musteri Musteri { get; set; } = new();

    public List<Ulke> UlkeList { get; set; } = new();
    public List<Sehir> SehirList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        UlkeList = await _context.Ulkeler
            .Where(u => u.IsActive)
            .OrderBy(u => u.UlkeAdi)
            .ToListAsync();

        SehirList = new();
        return Page();
    }

    // AJAX: /Musteriler/Create?handler=Sehirler&ulkeId=#
    public async Task<JsonResult> OnGetSehirlerAsync(int ulkeId)
    {
        var sehirler = await _context.Sehirler
            .Where(s => s.UlkeID == ulkeId && s.IsActive)
            .OrderBy(s => s.SehirAdi)
            .Select(s => new { s.SehirID, s.SehirAdi })
            .ToListAsync();

        return new JsonResult(sehirler);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // FirmaID claim’i zorunlu
        var firmaIdStr = User.FindFirstValue("FirmaID");
        if (string.IsNullOrEmpty(firmaIdStr) || !int.TryParse(firmaIdStr, out var firmaId))
        {
            ModelState.AddModelError(string.Empty, "Firma bilgisi bulunamadı.");
            UlkeList = await _context.Ulkeler.Where(u => u.IsActive).OrderBy(u => u.UlkeAdi).ToListAsync();
            SehirList = Musteri.UlkeID > 0
                ? await _context.Sehirler.Where(s => s.UlkeID == Musteri.UlkeID && s.IsActive).OrderBy(s => s.SehirAdi).ToListAsync()
                : new();
            return Page();
        }

        Musteri.FirmaID = firmaId;

        if (!ModelState.IsValid)
        {
            UlkeList = await _context.Ulkeler.Where(u => u.IsActive).OrderBy(u => u.UlkeAdi).ToListAsync();
            SehirList = Musteri.UlkeID > 0
                ? await _context.Sehirler.Where(s => s.UlkeID == Musteri.UlkeID && s.IsActive).OrderBy(s => s.SehirAdi).ToListAsync()
                : new();
            return Page();
        }

        _context.Musteriler.Add(Musteri);

        try
        {
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("UX_Musteriler_Firma_MusteriAdi"))
                ModelState.AddModelError("Musteri.MusteriAdi", "Bu müşteri adı zaten kayıtlı.");
            else
                ModelState.AddModelError(string.Empty, "Kaydetme sırasında bir hata oluştu.");

            UlkeList = await _context.Ulkeler.Where(u => u.IsActive).OrderBy(u => u.UlkeAdi).ToListAsync();
            SehirList = Musteri.UlkeID > 0
                ? await _context.Sehirler.Where(s => s.UlkeID == Musteri.UlkeID && s.IsActive).OrderBy(s => s.SehirAdi).ToListAsync()
                : new();
            return Page();
        }
    }
}
