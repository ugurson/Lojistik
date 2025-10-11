// Pages/Siparisler/Delete.cshtml.cs
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lojistik.Pages.Siparisler
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public record Item(
            int SiparisID,
            DateTime SiparisTarihi,
            string YukAciklamasi,
            string? Gonderen,
            string? Alici,
            decimal? Tutar,
            string? ParaBirimi,
            byte Durum
        );

        [BindProperty] public Item? Data { get; set; }

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
                    s.GonderenMusteri != null ? s.GonderenMusteri.MusteriAdi : null,
                    s.AliciMusteri != null ? s.AliciMusteri.MusteriAdi : null,
                    s.Tutar,
                    s.ParaBirimi,
                    s.Durum
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) Bu siparişten oluşan cari alacak kaydını sil
                //    Güvenli filtre: Firma + IlgiliSiparisID + IslemTuru='Sipariş' + Yonu=1
                await _context.Database.ExecuteSqlRawAsync(@"
DELETE FROM dbo.CariHareketler
WHERE FirmaID = {0}
  AND IlgiliSiparisID = {1}
  AND IslemTuru = N'Sipariş'
  AND Yonu = 1;",
                    firmaId, id);

                // 2) Siparişi ve ilişkili sevkiyat/sefer bağlarını sil
                var e = await _context.Siparisler
                    .Include(s => s.Sevkiyatlar)
                    .FirstOrDefaultAsync(s => s.FirmaID == firmaId && s.SiparisID == id);

                if (e != null)
                {
                    // SeferSevkiyat bağlantıları (toplu)
                    var sevIds = e.Sevkiyatlar.Select(s => s.SevkiyatID).ToList();
                    if (sevIds.Count > 0)
                    {
                        var baglantilar = await _context.SeferSevkiyatlar
                            .Where(x => sevIds.Contains(x.SevkiyatID))
                            .ToListAsync();

                        if (baglantilar.Count > 0)
                            _context.SeferSevkiyatlar.RemoveRange(baglantilar);

                        // Sevkiyatları sil
                        _context.Sevkiyatlar.RemoveRange(e.Sevkiyatlar);
                    }

                    // Siparişi sil
                    _context.Siparisler.Remove(e);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                    TempData["StatusMessage"] = "Sipariş ve ilişkili cari kaydı silindi.";
                }
                else
                {
                    // Sipariş bulunamadıysa sadece mesaj ver
                    await tx.RollbackAsync();
                    TempData["StatusMessage"] = "Sipariş bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["StatusMessage"] = "Silme sırasında hata: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage("./Index");
        }


    }
}
