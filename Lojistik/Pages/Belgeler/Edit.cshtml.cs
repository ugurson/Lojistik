using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;

namespace Lojistik.Pages.Belgeler
{
    public class EditModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public EditModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AracBelgesi AracBelgesi { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var firmaId = User.GetFirmaId();
            var aracbelgesi = await _context.AracBelgeleri
                .Include(b => b.Arac)
                .FirstOrDefaultAsync(m => m.BelgeID == id && m.Arac!.FirmaID == firmaId);
            if (aracbelgesi == null)
            {
                return NotFound();
            }
            AracBelgesi = aracbelgesi;
            ViewData["AracID"] = new SelectList(_context.Araclar.Where(a => a.FirmaID == firmaId), "AracID", "Plaka");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Kayıt bu firmaya ait mi kontrol et
            var firmaId = User.GetFirmaId();
            var sahiplik = await _context.AracBelgeleri
                .AnyAsync(b => b.BelgeID == AracBelgesi.BelgeID && b.Arac!.FirmaID == firmaId);
            if (!sahiplik) return NotFound();

            // Hedef AracID formdan geliyor — re-parenting'i engelle:
            // belgenin tasinacagi arac da bu firmaya ait olmali.
            var hedefAracAit = await _context.Araclar
                .AnyAsync(a => a.AracID == AracBelgesi.AracID && a.FirmaID == firmaId);
            if (!hedefAracAit) return Forbid();

            _context.Attach(AracBelgesi).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("/Araclar/Details", new { id = AracBelgesi.AracID });

            }
            catch (DbUpdateConcurrencyException)
            {
                {
                    if (!_context.AracBelgeleri.Any(e => e.BelgeID == AracBelgesi.BelgeID))
                        return NotFound();
                    else
                        throw;
                }
            }

        }

        private bool AracBelgesiExists(int id)
        {
            return _context.AracBelgeleri.Any(e => e.BelgeID == id);
        }
    }
}
