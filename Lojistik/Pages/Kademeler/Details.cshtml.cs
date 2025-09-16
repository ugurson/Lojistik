using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;
using Lojistik.Models;

namespace Lojistik.Pages.Kademeler
{
    public class DetailsModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public DetailsModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

        public AracKademe AracKademe { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var arackademe = await _context.AracKademeler.FirstOrDefaultAsync(m => m.KademeID == id);

            if (arackademe is not null)
            {
                AracKademe = arackademe;

                return Page();
            }

            return NotFound();
        }
    }
}
