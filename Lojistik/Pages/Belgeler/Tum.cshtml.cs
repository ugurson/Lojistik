using Lojistik.Data;
using Lojistik.Models;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Belgeler
{
    public class TumModel : PageModel
    {
        private readonly AppDbContext _context;
        public TumModel(AppDbContext context) => _context = context;

        public IList<AracBelgesi> Kayitlar { get; set; } = new List<AracBelgesi>();

        // Filtreler
        [BindProperty(SupportsGet = true)] public string? Plaka { get; set; }
        [BindProperty(SupportsGet = true)] public string? BelgeTipi { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? BaslangicMin { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? BaslangicMax { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? BitisMin { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? BitisMax { get; set; }

        // Paging
        [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));

        public async Task<IActionResult> OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var q = _context.AracBelgeleri
                .AsNoTracking()
                .Include(b => b.Arac)
                .Where(b => b.Arac!.FirmaID == firmaId);

            if (!string.IsNullOrWhiteSpace(Plaka))
                q = q.Where(b => b.Arac!.Plaka.Contains(Plaka));

            if (!string.IsNullOrWhiteSpace(BelgeTipi))
                q = q.Where(b => b.BelgeTipi.Contains(BelgeTipi));

            // >>>>> Tarih filtreleri: DateTime? -> DateOnly dönüşümü
            if (BaslangicMin.HasValue)
            {
                var d = DateOnly.FromDateTime(BaslangicMin.Value);
                q = q.Where(b => b.BaslangicTarihi >= d);
            }

            if (BaslangicMax.HasValue)
            {
                var d = DateOnly.FromDateTime(BaslangicMax.Value);
                q = q.Where(b => b.BaslangicTarihi <= d);
            }

            if (BitisMin.HasValue)
            {
                var d = DateOnly.FromDateTime(BitisMin.Value);
                q = q.Where(b => b.BitisTarihi != null && b.BitisTarihi >= d);
            }

            if (BitisMax.HasValue)
            {
                var d = DateOnly.FromDateTime(BitisMax.Value);
                q = q.Where(b => b.BitisTarihi != null && b.BitisTarihi <= d);
            }
            // <<<<<

            // Varsayılan sıralama
            q = q.OrderByDescending(b => b.BaslangicTarihi).ThenByDescending(b => b.BelgeID);

            TotalCount = await q.CountAsync();

            PageIndex = Math.Max(1, PageIndex);
            PageSize = Math.Max(1, PageSize);

            Kayitlar = await q
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

    }
}
