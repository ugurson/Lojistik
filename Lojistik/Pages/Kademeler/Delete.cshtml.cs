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

namespace Lojistik.Pages.Kademeler
{
    public class DeleteModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public DeleteModel(Lojistik.Data.AppDbContext context)
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

            var firmaId = User.GetFirmaId();
            var arackademe = await _context.AracKademeler
                .Include(k => k.Arac)
                .FirstOrDefaultAsync(m => m.KademeID == id && m.Arac!.FirmaID == firmaId);

            if (arackademe is not null)
            {
                AracKademe = arackademe;

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
            var arackademe = await _context.AracKademeler
                .Include(k => k.Arac)
                .FirstOrDefaultAsync(m => m.KademeID == id && m.Arac!.FirmaID == firmaId);
            if (arackademe != null)
            {
                AracKademe = arackademe;
                _context.AracKademeler.Remove(AracKademe);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
