using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Forwarding
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        public DeleteModel(AppDbContext context) => _context = context;

        public ForwardingIs Is { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var entity = await _context.ForwardingIsler
                .Include(f => f.Musteri)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.ForwardingID == id && f.FirmaID == firmaId);

            if (entity == null) return NotFound();
            Is = entity;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var firmaId = User.GetFirmaId();
            var entity = await _context.ForwardingIsler
                .FirstOrDefaultAsync(f => f.ForwardingID == id && f.FirmaID == firmaId);

            if (entity != null)
            {
                _context.ForwardingIsler.Remove(entity);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Forwarding işi silindi.";
            }

            return RedirectToPage("./Index");
        }
    }
}
