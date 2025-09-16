using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Belgeler;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    [BindProperty] public AracBelgesi AracBelgesi { get; set; } = new();

    [BindProperty] public IFormFile? Dosya { get; set; } 
    public string? Plaka { get; set; }
    private readonly IWebHostEnvironment _env;
    public CreateModel(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<IActionResult> OnGetAsync(int? aracId)
    {
        if (aracId is null) return BadRequest();

        var arac = await _context.Araclar.AsNoTracking()
                         .FirstOrDefaultAsync(a => a.AracID == aracId.Value);
        if (arac is null) return NotFound("Araç bulunamadı.");

        // Formda AracID otomatik dolsun
        AracBelgesi.AracID = arac.AracID;
        Plaka = arac.Plaka;
        AracBelgesi.BaslangicTarihi = DateOnly.FromDateTime(DateTime.Today);
        AracBelgesi.BitisTarihi = DateOnly.FromDateTime(DateTime.Today);

        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Eğer dosya yüklendiyse kaydet
        if (Dosya is not null && Dosya.Length > 0)
        {
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(Dosya.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError(nameof(Dosya), "Sadece PDF/JPG/PNG yükleyin.");
                return Page();
            }

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "belgeler");
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            using (var stream = System.IO.File.Create(filePath))
                await Dosya.CopyToAsync(stream);

            // Web'de erişilecek yol
            AracBelgesi.DosyaYolu = $"/uploads/belgeler/{fileName}";
        }

        // Aynı tip için aktif belge kontrolü
        if (AracBelgesi.BitisTarihi is null)
        {
            bool aktifVar = await _context.AracBelgeleri.AnyAsync(x =>
                x.AracID == AracBelgesi.AracID &&
                x.BelgeTipi == AracBelgesi.BelgeTipi &&
                x.BitisTarihi == null);

            if (aktifVar)
            {
                ModelState.AddModelError(string.Empty,
                    $"{AracBelgesi.BelgeTipi} için zaten aktif bir belge var.");
                return Page();
            }
        }

        _context.AracBelgeleri.Add(AracBelgesi);
        await _context.SaveChangesAsync();

        // Kaynak araca geri dön
        return RedirectToPage("/Araclar/Details", new { id = AracBelgesi.AracID });
    }
}
