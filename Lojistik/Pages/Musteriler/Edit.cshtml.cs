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

namespace Lojistik.Pages_Musteriler
{
    public class EditModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public EditModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Musteri Musteri { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musteri =  await _context.Musteri.FirstOrDefaultAsync(m => m.MusteriID == id);
            if (musteri == null)
            {
                return NotFound();
            }
            Musteri = musteri;
           ViewData["SehirID"] = new SelectList(_context.Set<Sehir>(), "SehirID", "SehirID");
           ViewData["UlkeID"] = new SelectList(_context.Set<Ulke>(), "UlkeID", "UlkeID");
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

            _context.Attach(Musteri).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MusteriExists(Musteri.MusteriID))
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

        private bool MusteriExists(int id)
        {
            return _context.Musteri.Any(e => e.MusteriID == id);
        }
    }
}
