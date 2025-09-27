using System;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Sevkiyatlar
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public record Item(
            int SevkiyatID,
            int SiparisID,
            string? CekiciPlaka,
            string? DorsePlaka,
            string? SurucuAdi,
            DateTime? YuklemeTarihi,
            DateTime? VarisTarihi,
            byte Durum
        );

        [BindProperty] public Item? Data { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!id.HasValue && int.TryParse(Request.Query["id"], out var idFromQuery))
                id = idFromQuery;
            if (!id.HasValue) return RedirectToPage("./Index");

            var firmaId = User.GetFirmaId();

            Data = await _context.Sevkiyatlar
                .AsNoTracking()
                .Where(s => s.FirmaID == firmaId && s.SevkiyatID == id.Value)
                .Select(s => new Item(
                    s.SevkiyatID,
                    s.SiparisID,
                    s.Arac != null ? s.Arac.Plaka : null,
                    s.Dorse != null ? s.Dorse.Plaka : null,
                    s.SurucuAdi,
                    s.YuklemeTarihi,
                    s.VarisTarihi,
                    s.Durum
                ))
                .FirstOrDefaultAsync();

            if (Data == null) return RedirectToPage("./Index");
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(int? id, int? siparisId)
        {
            if (!id.HasValue && int.TryParse(Request.Form["id"], out var idForm))
                id = idForm;
            if (!siparisId.HasValue && int.TryParse(Request.Form["siparisId"], out var spForm))
                siparisId = spForm;
            if (!id.HasValue) return RedirectToPage("./Index");

            // Eğer siparisId formdan gelmediyse DB’den çekelim (redirect için)
            if (!siparisId.HasValue)
            {
                siparisId = await _context.Sevkiyatlar
                    .Where(s => s.SevkiyatID == id.Value)
                    .Select(s => (int?)s.SiparisID)
                    .FirstOrDefaultAsync();
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) Önce SeferSevkiyatlar
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM [dbo].[SeferSevkiyatlar] WHERE [SevkiyatID] = {0}", id.Value);

                // 2) Sonra Sevkiyat
                var delSev = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM [dbo].[Sevkiyatlar] WHERE [SevkiyatID] = {0}", id.Value);

                await tx.CommitAsync();

                if (delSev == 0)
                    TempData["DelError"] = "Sevkiyat kaydı bulunamadı veya daha önce silinmiş.";

                return siparisId.HasValue
                    ? RedirectToPage("/Siparisler/Details", new { id = siparisId.Value })
                    : RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["DelError"] = ex.Message;
                return RedirectToPage("./Delete", new { id = id!.Value });
            }
        }
    }
}
