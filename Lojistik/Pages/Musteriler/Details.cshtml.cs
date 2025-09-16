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
    public class DetailsModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public DetailsModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

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
    }
}
