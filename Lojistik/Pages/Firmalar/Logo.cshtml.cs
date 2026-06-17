using Lojistik.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Firmalar
{
    public class LogoModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LogoModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firma = await _context.Firmalar.AsNoTracking()
                .Where(x => x.FirmaID == id && x.IsActive)
                .Select(x => new { x.LogoYolu, x.LogoMimeType })
                .FirstOrDefaultAsync();

            // default logo (wwwroot/images/logo21.png)
            var defaultLogoFull = Path.Combine(_env.WebRootPath, "images", "logo21.png");
            if (firma is null || string.IsNullOrWhiteSpace(firma.LogoYolu))
                return PhysicalFile(defaultLogoFull, "image/png");

            // LogoYolu: "/uploads/logolar/abc.png" veya "uploads/logolar/abc.png"
            var rel = firma.LogoYolu.Trim();
            rel = rel.TrimStart('~').TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());

            var full = Path.Combine(_env.WebRootPath, rel);
            if (!System.IO.File.Exists(full))
                return PhysicalFile(defaultLogoFull, "image/png");

            var ct = !string.IsNullOrWhiteSpace(firma.LogoMimeType)
                ? firma.LogoMimeType
                : Path.GetExtension(full).ToLowerInvariant() switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

            Response.Headers["Cache-Control"] = "public,max-age=3600";
            return PhysicalFile(full, ct);
        }
    }
}
