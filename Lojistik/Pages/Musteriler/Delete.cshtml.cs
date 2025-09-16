using System.Security.Claims;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Musteriler;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;
    public DeleteModel(AppDbContext context) => _context = context;

    [BindProperty]
    public Musteri Musteri { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var firmaIdStr = User.FindFirstValue("FirmaID");
        if (!int.TryParse(firmaIdStr, out var firmaId)) return Unauthorized();

        var m = await _context.Musteriler
            .Include(x => x.Ulke)
            .Include(x => x.Sehir)
            .FirstOrDefaultAsync(x => x.MusteriID == id && x.FirmaID == firmaId);

        if (m == null) return NotFound();

        Musteri = m;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var firmaIdStr = User.FindFirstValue("FirmaID");
        if (!int.TryParse(firmaIdStr, out var firmaId)) return Unauthorized();

        var m = await _context.Musteriler.FirstOrDefaultAsync(x => x.MusteriID == id && x.FirmaID == firmaId);
        if (m == null) return NotFound();

        _context.Musteriler.Remove(m);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
