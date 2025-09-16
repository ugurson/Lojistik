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
    public class IndexModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public IndexModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

        public IList<Musteri> Musteri { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Musteri = await _context.Musteri
                .Include(m => m.Sehir)
                .Include(m => m.Ulke).ToListAsync();
        }
    }
}
