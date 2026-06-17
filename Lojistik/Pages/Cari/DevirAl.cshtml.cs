using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lojistik.Pages.Cari
{
    public class DevirAlModel : PageModel
    {
        private readonly AppDbContext _context;
        public DevirAlModel(AppDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)] public int musteriId { get; set; }
        [BindProperty(SupportsGet = true)] public string pb { get; set; } = "";
        [BindProperty] public DateTime DevirTarihi { get; set; } = DateTime.Today.AddDays(-1);

        public string? MusteriAdi { get; set; }

        // Önizleme: para birimi → detaylı bakiye
        public class BakiyeSatir
        {
            public string ParaBirimi { get; set; } = "";
            public decimal ToplamBorc { get; set; }     // Yonu=1 → müşteri bize borçlu
            public decimal ToplamAlacak { get; set; }   // Yonu=0 → müşterinin ödemesi
            public decimal NetBakiye => ToplamBorc - ToplamAlacak;  // (+) müşteri borçlu, (-) müşteri alacaklı
            public int KayitSayisi { get; set; }
        }
        public List<BakiyeSatir> OnizlemeSatirlar { get; set; } = new();

        public string? StatusMessage { get; set; }

        // ── GET: Önizleme ─────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId  = User.GetUserId();

            // Sadece yetki2==2 kullanıcılar erişebilir
            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();
            if (yetki2 != 2) return Forbid();

            if (musteriId <= 0) return RedirectToPage("./Index");

            MusteriAdi = await _context.Musteriler.AsNoTracking()
                .Where(m => m.MusteriID == musteriId && m.FirmaID == firmaId)
                .Select(m => m.MusteriAdi)
                .FirstOrDefaultAsync() ?? $"#{musteriId}";

            // Tarihe kadar aktif hareketleri para birimi bazında özetle
            var ozet = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId
                          && ch.MusteriID == musteriId
                          && !ch.IsArsiv
                          && ch.Tarih <= DevirTarihi)
                .GroupBy(ch => ch.ParaBirimi)
                .Select(g => new
                {
                    ParaBirimi    = g.Key,
                    ToplamBorc    = g.Sum(x => x.Yonu == 1 ? x.Tutar : 0m),
                    ToplamAlacak  = g.Sum(x => x.Yonu == 0 ? x.Tutar : 0m),
                    KayitSayisi   = g.Count()
                })
                .ToListAsync();

            OnizlemeSatirlar = ozet.Select(x => new BakiyeSatir
            {
                ParaBirimi   = x.ParaBirimi,
                ToplamBorc   = x.ToplamBorc,
                ToplamAlacak = x.ToplamAlacak,
                KayitSayisi  = x.KayitSayisi
            }).OrderBy(x => x.ParaBirimi).ToList();

            return Page();
        }

        // ── POST: Devir İşlemini Gerçekleştir ─────────────────────────────────
        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId  = User.GetUserId();

            // Yetki kontrolü
            var yetki2 = await _context.Kullanicilar
                .Where(k => k.KullaniciID == userId && k.FirmaID == firmaId)
                .Select(k => k.YetkiSeviyesi2)
                .FirstOrDefaultAsync();
            if (yetki2 != 2) return Forbid();

            if (musteriId <= 0) return RedirectToPage("./Index");

            var musteriAit = await _context.Musteriler
                .AnyAsync(m => m.FirmaID == firmaId && m.MusteriID == musteriId);
            if (!musteriAit) return Forbid();

            if (DevirTarihi >= DateTime.Today)
            {
                ModelState.AddModelError(nameof(DevirTarihi), "Devir tarihi bugünden önce olmalıdır.");
                return await OnGetAsync() as PageResult ?? Page();
            }

            // Devir tarihi itibariyle aktif hareketleri para birimi bazında hesapla
            var ozet = await _context.CariHareketler.AsNoTracking()
                .Where(ch => ch.FirmaID == firmaId
                          && ch.MusteriID == musteriId
                          && !ch.IsArsiv
                          && ch.Tarih <= DevirTarihi)
                .GroupBy(ch => ch.ParaBirimi)
                .Select(g => new
                {
                    ParaBirimi  = g.Key,
                    NetBakiye   = g.Sum(x => x.Yonu == 1 ? x.Tutar : -x.Tutar),
                    KayitSayisi = g.Count()
                })
                .ToListAsync();

            if (!ozet.Any())
            {
                TempData["StatusMessage"] = "Seçilen tarihe kadar devredilecek hareket bulunamadı.";
                return RedirectToPage(new { musteriId, pb });
            }

            // Transaction: tüm işlemler ya hep başarılı ya hep iptal
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Bu müşteri için sıradaki DevirNo'yu hesapla (mevcut max + 1)
                var mevcutMax = await _context.CariHareketler
                    .Where(ch => ch.FirmaID == firmaId
                              && ch.MusteriID == musteriId
                              && ch.DevirNo != null)
                    .MaxAsync(ch => (int?)ch.DevirNo) ?? 0;

                int yeniDevirNo = mevcutMax + 1;

                foreach (var satirOzet in ozet)
                {
                    var tutar = Math.Abs(satirOzet.NetBakiye);

                    if (tutar == 0)
                    {
                        // Bakiye sıfırsa devir kaydı oluşturma, sadece arşivle
                    }
                    else
                    {
                        byte yonu = satirOzet.NetBakiye >= 0 ? (byte)1 : (byte)0;

                        var devirKaydi = new CariHareket
                        {
                            FirmaID              = firmaId,
                            MusteriID            = musteriId,
                            ParaBirimi           = satirOzet.ParaBirimi,
                            Tarih                = DevirTarihi.AddDays(1),
                            IslemTuru            = "DevirBakiye",
                            Aciklama             = $"Devir #{yeniDevirNo} – {DevirTarihi:dd.MM.yyyy} tarihi itibarıyla devredilen bakiye",
                            Yonu                 = yonu,
                            Tutar                = tutar,
                            Kur                  = null,
                            IsArsiv              = false,
                            DevirKapanmaTarihi   = DevirTarihi,
                            DevirNo              = yeniDevirNo,
                            CreatedByKullaniciID = userId,
                            CreatedAt            = DateTime.Now
                        };
                        _context.CariHareketler.Add(devirKaydi);
                    }
                }

                await _context.SaveChangesAsync();

                // Eski kayıtları arşivle: DevirNo'yu da yaz
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE dbo.CariHareketler
                    SET    IsArsiv = 1,
                           DevirNo = {3}
                    WHERE  FirmaID   = {0}
                      AND  MusteriID = {1}
                      AND  IsArsiv   = 0
                      AND  IslemTuru <> 'DevirBakiye'
                      AND  Tarih    <= {2}",
                    firmaId, musteriId, DevirTarihi.Date, yeniDevirNo);

                await transaction.CommitAsync();

                TempData["StatusMessage"] = $"Devir #{yeniDevirNo} tamamlandı. {DevirTarihi:dd.MM.yyyy} tarihine kadar {ozet.Sum(x => x.KayitSayisi)} kayıt arşivlendi.";
                return RedirectToPage("./Ekstre", new { musteriId, pb = pb.Length > 0 ? pb : ozet.First().ParaBirimi });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["StatusMessage"] = "Hata: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToPage(new { musteriId, pb });
            }
        }
    }
}
