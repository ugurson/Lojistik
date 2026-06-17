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

namespace Lojistik.Pages.Belgeler
{
    public class DetailsModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public DetailsModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

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

            if (aracbelgesi is not null)
            {
                AracBelgesi = aracbelgesi;

                return Page();
            }

            return NotFound();
        }
    }
}
