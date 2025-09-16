using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;
using Lojistik.Models;

namespace Lojistik.Pages_Musteriler
{
    public class DeleteModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public DeleteModel(Lojistik.Data.AppDbContext context)
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

            var musteri = await _context.Musteri.FirstOrDefaultAsync(m => m.MusteriID == id);

            if (musteri is not null)
            {
                Musteri = musteri;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musteri = await _context.Musteri.FindAsync(id);
            if (musteri != null)
            {
                Musteri = musteri;
                _context.Musteri.Remove(Musteri);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
