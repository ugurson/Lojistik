using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public bool HasSevkiyat { get; set; } = false;

        public record Item(
            int SiparisID,
            DateTime SiparisTarihi,
            string YukAciklamasi,
            int GonderenMusteriID,
            string? Gonderen,
            string? GonderenUlke,
            string? GonderenSehir,
            int AliciMusteriID,
            string? Alici,
            string? AliciUlke,
            string? AliciSehir,
            int? AraTedarikciMusteriID,
            string? AraTedarikci,
            int? Adet,
            string? AdetCinsi,
            int? Kilo,
            decimal? Tutar,
            string? ParaBirimi,
            string? FaturaNo,
            string? SubeKodu,
            string? Notlar,
            byte Durum,
            DateTime CreatedAt
        );

        public record SevkiyatRow(
            int SevkiyatID,
            string? DorsePlaka,
            DateTime? PlanlananYuklemeTarihi,
            DateTime? YuklemeTarihi,
            DateTime? VarisTarihi,
            byte Durum,
            string? CmrFile
        );

        public record SeferRow(
            int SeferID,
            string? SeferKodu,
            DateTime? CikisTarihi,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            byte Durum
        );

        public record CarilestirmeInfo(bool IsCarilestirildi, int? MusteriID, string? MusteriAdi, string? Taraf);
        public CarilestirmeInfo Cari { get; set; } = new(false, null, null, null);

        public Item? Data { get; set; }
        public List<SevkiyatRow> Sevkiyatlar { get; set; } = new();
        public List<SeferRow> Seferler { get; set; } = new();

        public async Task<IActionResult> OnPostSonlandirAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var s = await _context.Siparisler
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SiparisID == id);

            if (s == null)
                return RedirectToPage("./Index");

            if (s.Durum != 7)
            {
                s.Durum = 7;
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Sipariş sonlandırıldı (Durum = 7).";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            Data = await _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SiparisID == id)
                .Select(s => new Item(
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.YukAciklamasi,
                    s.GonderenMusteriID,
                    s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    s.GonderenMusteri != null ? s.GonderenMusteri.Ulke!.UlkeAdi : null,
                    s.GonderenMusteri != null ? s.GonderenMusteri.Sehir!.SehirAdi : null,
                    s.AliciMusteriID,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.Ulke!.UlkeAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.Sehir!.SehirAdi : null,
                    s.AraTedarikciMusteriID,
                    s.AraTedarikciMusteri != null ? s.AraTedarikciMusteri.MusteriAdi : null,
                    s.Adet,
                    s.AdetCinsi,
                    s.Kilo,
                    s.Tutar,
                    s.ParaBirimi,
                    s.FaturaNo,
                    s.SubeKodu,
                    s.Notlar,
                    s.Durum,
                    s.CreatedAt
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");

            Sevkiyatlar = await _context.Sevkiyatlar
                .AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.SiparisID == id)
                .OrderByDescending(x => x.SevkiyatID)
                .Select(x => new SevkiyatRow(
                    x.SevkiyatID,
                    x.Dorse != null ? x.Dorse.Plaka : null,
                    x.PlanlananYuklemeTarihi,
                    x.YuklemeTarihi,
                    x.VarisTarihi,
                    x.Durum,
                    x.CMRNo
                ))
                .ToListAsync();

            HasSevkiyat = Sevkiyatlar.Any();

            var seferFlat = await _context.SeferSevkiyatlar
                .AsNoTracking()
                .Where(x => x.FirmaID == firmaId && x.Sevkiyat.SiparisID == id)
                .Select(x => new
                {
                    x.Sefer.SeferID,
                    x.Sefer.SeferKodu,
                    x.Sefer.CikisTarihi,
                    Cekici = x.Sefer.Arac != null ? x.Sefer.Arac.Plaka : null,
                    Dorse = x.Sefer.Dorse != null ? x.Sefer.Dorse.Plaka : null,
                    x.Sefer.SurucuAdi,
                    x.Sefer.Durum
                })
                .ToListAsync();

            Seferler = seferFlat
                .GroupBy(a => a.SeferID)
                .Select(g =>
                {
                    var f = g.First();
                    return new SeferRow(
                        f.SeferID, f.SeferKodu, f.CikisTarihi,
                        f.Cekici, f.Dorse, f.SurucuAdi, f.Durum
                    );
                })
                .OrderByDescending(r => r.SeferID)
                .ToList();

            var cariRow = await _context.CariHareketler
                .AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId && ch.IlgiliSiparisID == id)
                .Select(ch => new { ch.MusteriID })
                .FirstOrDefaultAsync();

            if (cariRow != null)
            {
                var musteriAdi = await _context.Musteriler
                    .AsNoTracking()
                    .Where(m => m.MusteriID == cariRow.MusteriID)
                    .Select(m => m.MusteriAdi)
                    .FirstOrDefaultAsync();

                string taraf =
                    (cariRow.MusteriID == Data!.AliciMusteriID) ? "Alıcı" :
                    (cariRow.MusteriID == Data!.GonderenMusteriID) ? "Gönderen" :
                    (Data!.AraTedarikciMusteriID.HasValue && cariRow.MusteriID == Data!.AraTedarikciMusteriID.Value) ? "Ara Tedarikçi" :
                    "Diğer";

                Cari = new(true, cariRow.MusteriID, musteriAdi, taraf);
            }
            var yetki2 = await _context.Kullanicilar
    .Where(k => k.KullaniciID == User.GetUserId() && k.FirmaID == firmaId)
    .Select(k => k.YetkiSeviyesi2)
    .FirstOrDefaultAsync();
            ViewData["Yetki2"] = yetki2;
            return Page();
        }

        // ======================= CARI EKLERI =======================

        public class CarilestirInput
        {
            public int id { get; set; }               // SiparisID (hidden)
            public string? ParaBirimi { get; set; }   // Bilgi amaçlı
            public string? Aciklama { get; set; }
            public string? Hedef { get; set; }        // "Alici" | "Gonderen" | "Ara"
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCarilestirAsync(CarilestirInput input)
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            var siparis = await _context.Siparisler
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SiparisID == input.id)
                .Select(s => new
                {
                    s.SiparisID,
                    s.SiparisTarihi,
                    s.GonderenMusteriID,
                    s.AliciMusteriID,
                    s.AraTedarikciMusteriID,
                    s.Tutar,
                    s.ParaBirimi,
                    s.FaturaNo,
                    s.SubeKodu
                })
                .FirstOrDefaultAsync();

            var dorsePlaka = await _context.Sevkiyatlar
    .AsNoTracking()
    .Where(x => x.FirmaID == firmaId && x.SiparisID == siparis.SiparisID && x.DorseID != null)
    .OrderByDescending(x => x.SevkiyatID)
    .Select(x => x.Dorse!.Plaka)
    .FirstOrDefaultAsync();

            var aliciSehir = await _context.Musteriler
                .AsNoTracking()
                .Where(m => m.MusteriID == siparis.AliciMusteriID)
                .Select(m => m.Sehir != null ? m.Sehir.SehirAdi : null)
                .FirstOrDefaultAsync();

            if (siparis is null)
            {
                TempData["StatusMessage"] = "Sipariş bulunamadı.";
                return RedirectToPage(new { id = input.id });
            }
            if (siparis.Tutar is null || siparis.Tutar <= 0)
            {
                TempData["StatusMessage"] = "Sipariş tutarı geçersiz.";
                return RedirectToPage(new { id = input.id });
            }

            var varMi = await _context.CariHareketler
                .AsNoTracking()
                .AnyAsync(ch => ch.FirmaID == firmaId && ch.IlgiliSiparisID == siparis.SiparisID);

            if (varMi)
            {
                TempData["StatusMessage"] = "Bu sipariş daha önce cariye işlenmiş.";
                return RedirectToPage(new { id = input.id });
            }

            var pb = (siparis.ParaBirimi ?? "TL").Trim().ToUpperInvariant();
            object? kurParam = null;
            object? subeParam = string.IsNullOrWhiteSpace(siparis.SubeKodu) ? null : siparis.SubeKodu;
            object? evrakNoParam = string.IsNullOrWhiteSpace(siparis.FaturaNo) ? null : siparis.FaturaNo;

            int? hedefMusteriId = input.Hedef?.ToLowerInvariant() switch
            {
                "gonderen" => (int?)siparis.GonderenMusteriID,
                "ara" => siparis.AraTedarikciMusteriID,
                _ => (int?)siparis.AliciMusteriID
            };

            if (hedefMusteriId == null || hedefMusteriId <= 0)
            {
                TempData["StatusMessage"] = "Seçilen cari tarafı geçersiz (müşteri bulunamadı).";
                return RedirectToPage(new { id = input.id });
            }

            var aciklama = $"{(dorsePlaka ?? "—")} - {(aliciSehir ?? "—")}";

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var sql =
$@"
INSERT INTO dbo.CariHareketler
(
  FirmaID, SubeKodu, KullaniciID, MusteriID,
  IlgiliSiparisID, IlgiliSevkiyatID,
  Tarih,
  IslemTuru, EvrakNo, Aciklama,
  ParaBirimi, Yonu, Tutar, Kur, CreatedByKullaniciID
)
VALUES
(
  {{0}}, {{1}}, {{2}}, {{3}},
  {{4}}, NULL,
  {{5}},
  N'Sipariş', {{6}}, {{7}},
  {{8}}, 1, {{9}}, {{10}}, {{11}}
);
";

                var parameters = new object?[]
                {
                    firmaId,               // 0
                    subeParam,             // 1
                    userId,                // 2
                    hedefMusteriId,        // 3
                    siparis.SiparisID,     // 4
                    siparis.SiparisTarihi, // 5
                    evrakNoParam,          // 6
                    aciklama,              // 7
                    pb,                    // 8
                    siparis.Tutar!.Value,  // 9
                    kurParam,              // 10
                    userId                 // 11
                };

                await _context.Database.ExecuteSqlRawAsync(sql, parameters);

                // --- SİPARİŞİ DENORMALIZE GÜNCELLE ---
                await _context.Database.ExecuteSqlRawAsync(@"
UPDATE dbo.Siparisler
SET IsCariles = 1,
    CariIslenenMusteriID = {0},
    CariEvrakNo = {1},
    CariIslemTarihi = {2},
    CarilesByKullaniciID = {3}
WHERE FirmaID = {4} AND SiparisID = {5};",
                    hedefMusteriId,            // {0}
                    evrakNoParam,              // {1}
                    siparis.SiparisTarihi,     // {2} -> CH'de kullandığın tarih
                    userId,                    // {3}
                    firmaId,                   // {4}
                    siparis.SiparisID          // {5}
                );

                await tx.CommitAsync();

                TempData["StatusMessage"] = "Sipariş cariye işlendi.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["StatusMessage"] = "Carileştirme sırasında hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new { id = input.id });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCariCikartAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var siparisVarMi = await _context.Siparisler
                .AsNoTracking()
                .AnyAsync(s => s.FirmaID == firmaId && s.SiparisID == id);

            if (!siparisVarMi)
            {
                TempData["StatusMessage"] = "Sipariş bulunamadı.";
                return RedirectToPage("./Index");
            }

            try
            {
                var etkilenen = await _context.Database.ExecuteSqlRawAsync(@"
DELETE FROM dbo.CariHareketler
WHERE FirmaID = {0}
  AND IlgiliSiparisID = {1}
  AND IslemTuru = N'Sipariş'
  AND Yonu = 1;",
                    firmaId, id);

                if (etkilenen > 0)
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
UPDATE dbo.Siparisler
SET IsCariles = 0,
    CariIslenenMusteriID = NULL,
    CariEvrakNo = NULL,
    CariIslemTarihi = NULL,
    CarilesByKullaniciID = NULL
WHERE FirmaID = {0} AND SiparisID = {1};", firmaId, id);
                }

                TempData["StatusMessage"] = etkilenen > 0
                    ? "Cariye işlenen kayıt kaldırıldı."
                    : "Bu sipariş cariye işlenmemiş.";
            }
            catch (Exception ex)
            {
                TempData["StatusMessage"] = "İşlem sırasında hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new { id });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostGeriAktifEtAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();

            if (yetki2 != 2)
            {
                TempData["StatusMessage"] = "Yetkiniz yok (YetkiSeviyesi2=2 gerekli).";
                return RedirectToPage(new { id });
            }

            var s = await _context.Siparisler
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SiparisID == id);

            if (s is null) return RedirectToPage("./Index");

            if (s.Durum != 7)
            {
                TempData["StatusMessage"] = "Sipariş sonlandırılmış değil.";
                return RedirectToPage(new { id });
            }

            s.Durum = 2; // sabit
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Sipariş tekrar aktif yapıldı (Durum = 2).";
            return RedirectToPage(new { id });
        }

    }
}
