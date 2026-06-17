using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Lojistik.Pages.Kademeler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public IList<AracKademe> Kademeler { get; set; } = new List<AracKademe>();

        [BindProperty(SupportsGet = true)] public int? AracID { get; set; } // [YENİ]
        public string? Plaka { get; set; } // [YENİ]

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? Baslangic { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? Bitis { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SadeceOdenmeyen { get; set; } = false;


        public async Task OnGetAsync(int? aracId, string? plaka)
        {
            AracID = aracId ?? AracID;
            Plaka = plaka;

            var firmaIdStr = User.FindFirst("FirmaID")?.Value;
            int.TryParse(firmaIdStr, out var firmaId);

            var today = DateTime.Today;
            Baslangic ??= new DateTime(today.Year, today.Month, 1);
            Bitis ??= today;

            IQueryable<AracKademe> q = _context.AracKademeler
                .Include(k => k.Arac)
                .Where(k => k.Arac != null && k.Arac.FirmaID == firmaId)
                .OrderByDescending(k => k.Tarih);

            q = q.Where(k => k.Tarih >= Baslangic.Value && k.Tarih <= Bitis.Value);
            if (SadeceOdenmeyen) q = q.Where(k => k.Odeme == 0);

            if (AracID.HasValue)
            {
                q = q.Where(k => k.AracID == AracID.Value);

                if (string.IsNullOrWhiteSpace(Plaka))
                {
                    Plaka = await _context.Araclar
                        .Where(a => a.AracID == AracID.Value && a.FirmaID == firmaId)
                        .Select(a => a.Plaka)
                        .FirstOrDefaultAsync();
                }
            }

            Kademeler = await q.ToListAsync();
        }

        public async Task<IActionResult> OnPostOdemeAsync(
            int id,
            int? aracId,
            string? plaka,
            DateTime? baslangic,
            DateTime? bitis,
            bool sadeceOdenmeyen)
        {
            var firmaId = User.GetFirmaId();

            var row = await _context.AracKademeler
                .FirstOrDefaultAsync(x => x.KademeID == id);

            if (row == null) return NotFound();

            var aracFirmaId = await _context.Araclar
                .Where(a => a.AracID == row.AracID)
                .Select(a => a.FirmaID)
                .FirstOrDefaultAsync();

            if (aracFirmaId != firmaId) return NotFound();

            row.Odeme = row.Odeme == 1 ? 0 : 1;
            await _context.SaveChangesAsync();

            return RedirectToPage(new
            {
                aracId,
                plaka,
                Baslangic = baslangic,
                Bitis = bitis,
                SadeceOdenmeyen = sadeceOdenmeyen
            });
        }



    }
}
