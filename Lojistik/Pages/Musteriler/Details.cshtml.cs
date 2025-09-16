using System.Security.Claims;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Musteriler;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context) => _context = context;

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
}
