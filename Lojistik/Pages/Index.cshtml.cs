using Lojistik.Data;
using Lojistik.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly AppDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Güncelleme notları
        public List<Lojistik.Models.GuncellemeNotu> GuncellemeNotlari { get; set; } = new();

        // Dashboard metrikleri
        public int BugünAcilanSiparis    { get; set; }
        public int AcikSiparisSayisi     { get; set; }
        public int AktifSeferSayisi      { get; set; }
        public int SuresiBitenBelgeSayisi   { get; set; }  // BitisTarihi <= bugün
        public int YaklasanBelgeSayisi      { get; set; }  // bugün < BitisTarihi <= +30 gün
        public Dictionary<string, decimal> Son30GunYakit  { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return;

            int firmaId;
            try { firmaId = User.GetFirmaId(); }
            catch { return; }

            var today      = DateTime.Today;
            var limit30    = today.AddDays(-30);
            var todayDO    = DateOnly.FromDateTime(today);
            var limitDO    = DateOnly.FromDateTime(today.AddDays(30));

            // 1. Bugün oluşturulan siparişler
            BugünAcilanSiparis = await _context.Siparisler
                .AsNoTracking()
                .CountAsync(s => s.FirmaID == firmaId
                              && s.CreatedAt >= today
                              && s.CreatedAt < today.AddDays(1));

            // 2. Açık siparişler (Durum < 7)
            AcikSiparisSayisi = await _context.Siparisler
                .AsNoTracking()
                .CountAsync(s => s.FirmaID == firmaId && s.Durum < 7);

            // 3. Aktif seferler (Yeni=0 veya Planlandi=1)
            AktifSeferSayisi = await _context.Seferler
                .AsNoTracking()
                .CountAsync(s => s.FirmaID == firmaId
                              && (s.Durum == 0 || s.Durum == 1));

            // 4a. Süresi BİTEN araç belgeleri (BitisTarihi <= bugün)
            SuresiBitenBelgeSayisi = await _context.AracBelgeleri
                .AsNoTracking()
                .CountAsync(b => b.Arac!.FirmaID == firmaId
                              && b.BitisTarihi.HasValue
                              && b.BitisTarihi <= todayDO);

            // 4b. 30 gün içinde bitecek araç belgeleri (bugün < BitisTarihi <= +30 gün)
            YaklasanBelgeSayisi = await _context.AracBelgeleri
                .AsNoTracking()
                .CountAsync(b => b.Arac!.FirmaID == firmaId
                              && b.BitisTarihi.HasValue
                              && b.BitisTarihi > todayDO
                              && b.BitisTarihi <= limitDO);

            // 0. Güncelleme notları (aktif, en yeni 20)
            GuncellemeNotlari = await _context.GuncellemeNotlari
                .AsNoTracking()
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.Tarih)
                .ThenByDescending(n => n.GuncellemeNotuID)
                .Take(20)
                .ToListAsync();

            // 5. Son 30 günün yakıt maliyeti (para birimi bazında)
            Son30GunYakit = (await _context.SeferMasraflari
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId
                         && m.Tarih >= limit30
                         && m.MasrafTipi.ToLower().Contains("yakıt"))
                .GroupBy(m => m.ParaBirimi)
                .Select(g => new { PB = g.Key, Toplam = g.Sum(m => m.Tutar) })
                .ToListAsync())
                .ToDictionary(g => g.PB, g => g.Toplam);

        }
    }
}
