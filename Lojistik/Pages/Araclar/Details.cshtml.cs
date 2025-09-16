using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;
using Lojistik.Models;

namespace Lojistik.Pages.Araclar
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Arac Arac { get; set; } = default!;
        public List<AracBelgesi> Belgeler { get; set; } = new();
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var arac = await _context.Araclar.AsNoTracking()
                        .FirstOrDefaultAsync(m => m.AracID == id.Value);



            if (arac == null) return NotFound();

            Arac = arac;

            Belgeler = await _context.AracBelgeleri.AsNoTracking()
                          .Where(b => b.AracID == Arac.AracID)
                          .OrderByDescending(b => b.BaslangicTarihi)
                          .ToListAsync();

            return Page();
        }
    }
}
