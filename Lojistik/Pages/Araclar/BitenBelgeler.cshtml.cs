using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Models;
using Lojistik.Extensions;                 // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Araclar
{
    public class BitenBelgelerModel : PageModel
    {
        private readonly AppDbContext _context;
        public BitenBelgelerModel(AppDbContext context) => _context = context;

        public List<AracBelgesi> Bitenler { get; set; } = new();
        public List<AracBelgesi> Yaklasanlar { get; set; } = new();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var limit = today.AddDays(30);
            int firmaId = User.GetFirmaId();

            // ✅ Firma filtresi en başta (Include sadece tabloda Plaka vb. göstermek için)
            var baseQuery = _context.AracBelgeleri
                                    .Include(b => b.Arac)
                                    .AsNoTracking()
                                    .Where(b => b.Arac.FirmaID == firmaId && b.BitisTarihi != null);

            // Süresi bitenler (Bitis <= bugün)
            Bitenler = await baseQuery
                .Where(b => b.BitisTarihi! <= today)
                .OrderBy(b => b.BitisTarihi)
                .ToListAsync();

            // 30 gün içinde bitecek (Bugün < Bitis <= 30 gün)
            Yaklasanlar = await baseQuery
                .Where(b => b.BitisTarihi! > today && b.BitisTarihi! <= limit)
                .OrderBy(b => b.BitisTarihi)
                .ToListAsync();
        }
    }
}
