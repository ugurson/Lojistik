using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;


namespace Lojistik.Pages.Araclar;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    public EditModel(AppDbContext context) => _context = context;

    // Sadece görüntülemek için bind ediyoruz (POST'ta DB'den çekip güncelleyeceğiz)
    [BindProperty]
    public Arac Arac { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        int firmaId = User.GetFirmaId();

        Arac = await _context.Araclar
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AracID == id && a.FirmaID == firmaId);

        if (Arac == null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid) return Page();

        int firmaId = User.GetFirmaId();

        // 1) KAYDI firma filtresi ile DB’den al (başka firmanın kaydına izin verme)
        var arac = await _context.Araclar
            .FirstOrDefaultAsync(a => a.AracID == id && a.FirmaID == firmaId);

        if (arac == null) return NotFound();

        // 2) SADECE izin verilen alanları güncelle (FirmaID/CreatedBy asla değişmesin)
        var updated = await TryUpdateModelAsync<Arac>(
            arac,                  // var olan entity
            "Arac",                // form prefix: Arac.*
            a => a.Plaka,
            a => a.Marka,
            a => a.Model,
            a => a.ModelYili,
            a => a.AracTipi,
            a => a.IsDorse,
            a => a.Durum,
            a => a.Notlar
        );

        if (!updated) return Page();

        try
        {
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Araclar_Plaka") == true)
        {
            // Unique plaka ihlali için kullanıcı dostu mesaj
            ModelState.AddModelError("Arac.Plaka", "Bu plaka zaten kayıtlı.");
            return Page();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Araclar_Firmalar") == true)
        {
            // FirmaID düşerse/bozulursa
            ModelState.AddModelError(string.Empty, "Firma bilgisi hatalı görünüyor. Lütfen tekrar deneyin.");
            return Page();
        }
    }
}
