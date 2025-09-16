using System.Security.Claims;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Musteriler;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    public EditModel(AppDbContext context) => _context = context;

    [BindProperty]
    public Musteri Musteri { get; set; } = new();

    public List<Ulke> UlkeList { get; set; } = new();
    public List<Sehir> SehirList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Firma filtresi
        var firmaIdStr = User.FindFirstValue("FirmaID");
        if (!int.TryParse(firmaIdStr, out var firmaId)) return Unauthorized();

        var m = await _context.Musteriler
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MusteriID == id && x.FirmaID == firmaId);

        if (m == null) return NotFound();

        Musteri = m;

        UlkeList = await _context.Ulkeler
            .Where(u => u.IsActive)
            .OrderBy(u => u.UlkeAdi)
            .ToListAsync();

        SehirList = await _context.Sehirler
            .Where(s => s.UlkeID == Musteri.UlkeID && s.IsActive)
            .OrderBy(s => s.SehirAdi)
            .ToListAsync();

        return Page();
    }

    // AJAX: /Musteriler/Edit?handler=Sehirler&ulkeId=#
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
        var firmaIdStr = User.FindFirstValue("FirmaID");
        if (!int.TryParse(firmaIdStr, out var firmaId)) return Unauthorized();

        if (!ModelState.IsValid)
        {
            UlkeList = await _context.Ulkeler.Where(u => u.IsActive).OrderBy(u => u.UlkeAdi).ToListAsync();
            SehirList = Musteri.UlkeID > 0
                ? await _context.Sehirler.Where(s => s.UlkeID == Musteri.UlkeID && s.IsActive).OrderBy(s => s.SehirAdi).ToListAsync()
                : new();
            return Page();
        }

        // Kayıt gerçekten bu firmaya mı ait?
        var belongs = await _context.Musteriler.AnyAsync(x => x.MusteriID == Musteri.MusteriID && x.FirmaID == firmaId);
        if (!belongs) return NotFound();

        // FirmaID dışardan değiştirilmesin
        _context.Attach(Musteri).Property(x => x.FirmaID).IsModified = false;
        _context.Entry(Musteri).State = EntityState.Modified;

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
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");

            UlkeList = await _context.Ulkeler.Where(u => u.IsActive).OrderBy(u => u.UlkeAdi).ToListAsync();
            SehirList = Musteri.UlkeID > 0
                ? await _context.Sehirler.Where(s => s.UlkeID == Musteri.UlkeID && s.IsActive).OrderBy(s => s.SehirAdi).ToListAsync()
                : new();
            return Page();
        }
    }
}
