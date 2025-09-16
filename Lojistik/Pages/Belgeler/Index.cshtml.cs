using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Lojistik.Data;
using Lojistik.Models;
using Lojistik.Extensions; // GetFirmaId()

namespace Lojistik.Pages.Belgeler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public IList<AracBelgesi> AracBelgesi { get; set; } = new List<AracBelgesi>();

        // URL'den gelebilen bağlam: sadece bu aracın belgeleri gösterilsin
        [BindProperty(SupportsGet = true)]
        public int? AracId { get; set; }

        public async Task OnGetAsync()
        {
            int firmaId = User.GetFirmaId();

            // 1) Firma filtresi ZORUNLU
            IQueryable<AracBelgesi> q = _context.AracBelgeleri
                .Include(b => b.Arac)
                .AsNoTracking()
                .Where(b => b.Arac.FirmaID == firmaId);

            // 2) (opsiyonel) Araç bağlamı
            if (AracId.HasValue)
                q = q.Where(b => b.AracID == AracId.Value);

            // 3) Sıralama (dilersen değiştir)
            AracBelgesi = await q
                .OrderByDescending(b => b.BitisTarihi)
                .ThenBy(b => b.Arac.Plaka)
                .ToListAsync();
        }
    }
}
