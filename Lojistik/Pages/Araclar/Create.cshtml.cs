using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Lojistik.Data;
using Lojistik.Models;
using Lojistik.Extensions;

namespace Lojistik.Pages.Araclar
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public List<SelectListItem> AracTipleri { get; set; } = new();
        public List<SelectListItem> Durumlar { get; set; } = new();
        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            AracTipleri = new List<SelectListItem>
    {
        new("Tır (Çekici)", "Tır"),
        new("Kamyon", "Kamyon"),
        new("Kamyonet", "Kamyonet"),
        new("Dorse", "Dorse"),
        new("Araç", "Araç"),
    };

            // Durum listesi
            Durumlar = new List<SelectListItem>
    {
        new("Aktif", "Aktif"),
        new("Pasif", "Pasif")
    };
            return Page();
        }

        [BindProperty]
        public Arac Arac { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadSelectLists();
                return Page();
            }
            // FirmaID’yi login olan kullanıcıya göre ayarla
            Arac.FirmaID = User.GetFirmaId();
            Arac.CreatedByKullaniciID = User.GetUserId();

            _context.Araclar.Add(Arac);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
        private void LoadSelectLists()
        {
            AracTipleri = new List<SelectListItem>
            {
                new("Tır (Çekici)", "Tır"),
                new("Kamyon", "Kamyon"),
                new("Kamyonet", "Kamyonet"),
                new("Dorse", "Dorse"),
                new("Araç", "Araç"),
            };

            Durumlar = new List<SelectListItem>
            {
                new("Aktif", "Aktif"),
                new("Pasif", "Pasif")
            };
        }
    }
}
