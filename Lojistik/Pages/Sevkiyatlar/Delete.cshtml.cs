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

            var firmaId = User.GetFirmaId();

            // Kaydın bu firmaya ait olduğunu doğrula — ait değilse işlem yapma
            var sevkiyat = await _context.Sevkiyatlar
                .AsNoTracking()
                .Where(s => s.SevkiyatID == id.Value && s.FirmaID == firmaId)
                .Select(s => new { s.SiparisID })
                .FirstOrDefaultAsync();

            if (sevkiyat == null) return NotFound();

            // siparisId formdan gelmemişse doğrulanmış DB kaydından al
            if (!siparisId.HasValue)
                siparisId = sevkiyat.SiparisID;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) Önce SeferSevkiyatlar — FirmaID join ile kısıtla
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE ss FROM [dbo].[SeferSevkiyatlar] ss" +
                    " INNER JOIN [dbo].[Sevkiyatlar] s ON s.SevkiyatID = ss.SevkiyatID" +
                    " WHERE ss.SevkiyatID = {0} AND s.FirmaID = {1}", id.Value, firmaId);

                // 2) Sonra Sevkiyat — FirmaID şartı ile
                var delSev = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM [dbo].[Sevkiyatlar] WHERE [SevkiyatID] = {0} AND [FirmaID] = {1}", id.Value, firmaId);

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
