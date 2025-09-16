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

namespace Lojistik.Pages.Kademeler
{
    public class EditModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public EditModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AracKademe AracKademe { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var arackademe =  await _context.AracKademeler.FirstOrDefaultAsync(m => m.KademeID == id);
            if (arackademe == null)
            {
                return NotFound();
            }
            AracKademe = arackademe;
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

            _context.Attach(AracKademe).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AracKademeExists(AracKademe.KademeID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool AracKademeExists(int id)
        {
            return _context.AracKademeler.Any(e => e.KademeID == id);
        }
    }
}
