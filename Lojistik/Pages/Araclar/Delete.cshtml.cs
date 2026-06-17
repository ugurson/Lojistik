using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;

namespace Lojistik.Pages.Araclar
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Arac Arac { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var firmaId = User.GetFirmaId();
            var arac = await _context.Araclar.FirstOrDefaultAsync(m => m.AracID == id && m.FirmaID == firmaId);

            if (arac is not null)
            {
                Arac = arac;

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

            var firmaId = User.GetFirmaId();
            var arac = await _context.Araclar.FirstOrDefaultAsync(m => m.AracID == id && m.FirmaID == firmaId);
            if (arac != null)
            {
                Arac = arac;
                _context.Araclar.Remove(Arac);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
