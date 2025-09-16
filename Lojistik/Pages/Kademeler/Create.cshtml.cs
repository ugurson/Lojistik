using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Kademeler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public AracKademe Kademe { get; set; } = new(); // [YENİ]
        public string? Plaka { get; set; } // [YENİ]

        public async Task<IActionResult> OnGetAsync(int? aracId, string? plaka)
        {
            if (aracId.HasValue)
            {
                Kademe.AracID = aracId.Value;     // [YENİ]
                Plaka = plaka ?? await _context.Araclar
                    .Where(a => a.AracID == aracId.Value)
                    .Select(a => a.Plaka)
                    .FirstOrDefaultAsync();
            }
            // Varsayılanlar
            Kademe.Tarih = DateTime.Today; // [YENİ]
            Kademe.ParaBirimi = "TRY";     // [YENİ]
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // CreatedAt DB tarafında DEFAULT GETDATE() ile set edilecek.
            _context.AracKademeler.Add(Kademe);
            await _context.SaveChangesAsync();

            // Kayıttan sonra aynı aracın listesine dön
            if (Kademe.AracID > 0)
            {
                var plaka = await _context.Araclar
                    .Where(a => a.AracID == Kademe.AracID)
                    .Select(a => a.Plaka)
                    .FirstOrDefaultAsync();

                return RedirectToPage("Index", new { aracId = Kademe.AracID, plaka });
            }

            return RedirectToPage("Index");
        }
    }
}
