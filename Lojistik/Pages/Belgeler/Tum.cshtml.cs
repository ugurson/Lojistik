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

        // Sıralama
        [BindProperty(SupportsGet = true)] public string? SortBy { get; set; }   // "baslangic" | "bitis"
        [BindProperty(SupportsGet = true)] public string? SortDir { get; set; }  // "asc" | "desc"

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));

        public async Task<IActionResult> OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var q = _context.AracBelgeleri
                .AsNoTracking()
                .Include(b => b.Arac)
                .Where(b => b.Arac!.FirmaID == firmaId);

            // EF.Functions.Collate: DB/kolon collation'ı SQL_Latin1_General_CP1_CI_AS (Türkçe değil),
            // bu yüzden ş/Ş, ı/İ, ğ/Ğ gibi harfler case-insensitive eşleşmiyor. Arama anında
            // Turkish_CI_AS'a geçici olarak zorluyoruz (şema/kolon değişmiyor).
            if (!string.IsNullOrWhiteSpace(Plaka))
                q = q.Where(b => EF.Functions.Collate(b.Arac!.Plaka, "Turkish_CI_AS").Contains(Plaka));

            if (!string.IsNullOrWhiteSpace(BelgeTipi))
                q = q.Where(b => EF.Functions.Collate(b.BelgeTipi, "Turkish_CI_AS").Contains(BelgeTipi));

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

            // Sıralama (varsayılan: Başlangıç Tarihi, azalan)
            bool asc = string.Equals(SortDir, "asc", StringComparison.OrdinalIgnoreCase);
            q = SortBy?.ToLower() switch
            {
                "bitis" => asc
                    ? q.OrderBy(b => b.BitisTarihi).ThenByDescending(b => b.BelgeID)
                    : q.OrderByDescending(b => b.BitisTarihi).ThenByDescending(b => b.BelgeID),
                "baslangic" when asc
                    => q.OrderBy(b => b.BaslangicTarihi).ThenByDescending(b => b.BelgeID),
                _ => q.OrderByDescending(b => b.BaslangicTarihi).ThenByDescending(b => b.BelgeID)
            };

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
