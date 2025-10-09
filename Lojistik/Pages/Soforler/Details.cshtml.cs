using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Soforler
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public Sofor Data { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var sofor = await _context.Soforler.AsNoTracking().FirstOrDefaultAsync(x => x.SoforID == id && x.FirmaID == firmaId);
            if (sofor == null) return NotFound();
            Data = sofor;
            return Page();
        }
    }
}
