using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Lojistik.Data;
using Lojistik.Models;

namespace Lojistik.Pages_Musteriler
{
    public class CreateModel : PageModel
    {
        private readonly Lojistik.Data.AppDbContext _context;

        public CreateModel(Lojistik.Data.AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["SehirID"] = new SelectList(_context.Set<Sehir>(), "SehirID", "SehirID");
        ViewData["UlkeID"] = new SelectList(_context.Set<Ulke>(), "UlkeID", "UlkeID");
            return Page();
        }

        [BindProperty]
        public Musteri Musteri { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Musteri.Add(Musteri);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
