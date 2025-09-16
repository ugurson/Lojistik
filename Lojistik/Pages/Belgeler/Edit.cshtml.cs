using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;
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

            var aracbelgesi =  await _context.AracBelgeleri.FirstOrDefaultAsync(m => m.BelgeID == id);
            if (aracbelgesi == null)
            {
                return NotFound();
            }
            AracBelgesi = aracbelgesi;
           ViewData["AracID"] = new SelectList(_context.Araclar, "AracID", "Plaka");
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
