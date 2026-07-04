using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lojistik.Pages.SeferMasraflari
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? ParaBirimleri { get; set; }
        public SelectList? MasrafTipleri { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }
            [DataType(DataType.Date)] public DateTime Tarih { get; set; } = DateTime.Today;
            [Required, StringLength(50)] public string MasrafTipi { get; set; } = "Yakıt";
            [Range(0, 999999999)] public decimal Tutar { get; set; }
            [Required, StringLength(10)] public string ParaBirimi { get; set; } = "TL";
            [StringLength(50)] public string? FaturaBelgeNo { get; set; }
            [StringLength(50)] public string? Ulke { get; set; }
            [StringLength(100)] public string? Yer { get; set; }
            [StringLength(300)] public string? Notlar { get; set; }
            [Range(0, 999999)] public decimal? YakitLitre { get; set; }

        }

        public async Task<IActionResult> OnGetAsync(int seferId)
        {
            if (seferId <= 0) return RedirectToPage("/Seferler/Index");

            Input.SeferID = seferId;

            ParaBirimleri = new SelectList(new[] { "TL", "EUR", "USD"  });
            await LoadMasrafTipleriAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var subeKodu = User.GetSubeKodu();
            var userId = User.GetUserId();

            var tip = (Input.MasrafTipi ?? "").Trim();

            if (tip == "Yakıt" && (Input.YakitLitre is null || Input.YakitLitre <= 0))
            {
                ModelState.AddModelError("Input.YakitLitre", "Yakıt masrafında litre zorunludur.");
            }


            if (!ModelState.IsValid)
            {
                ParaBirimleri = new SelectList(new[] { "TL", "EUR", "USD" });
                await LoadMasrafTipleriAsync();
                return Page();
            }

            var seferAit = await _context.Seferler
                .AnyAsync(s => s.FirmaID == firmaId && s.SeferID == Input.SeferID);
            if (!seferAit) return Forbid();

            var entity = new SeferMasraf
            {
                FirmaID = firmaId,
                SubeKodu = subeKodu,
                KullaniciID = userId,
                SeferID = Input.SeferID,
                Tarih = Input.Tarih,
                MasrafTipi = Input.MasrafTipi.Trim(),
                Tutar = Input.Tutar,
                ParaBirimi = Input.ParaBirimi.Trim(),
                YakitLitre = (tip == "Yakıt") ? Input.YakitLitre : null,
                FaturaBelgeNo = string.IsNullOrWhiteSpace(Input.FaturaBelgeNo) ? null : Input.FaturaBelgeNo.Trim(),
                Ulke = string.IsNullOrWhiteSpace(Input.Ulke) ? null : Input.Ulke.Trim(),
                Yer = string.IsNullOrWhiteSpace(Input.Yer) ? null : Input.Yer.Trim(),
                Notlar = string.IsNullOrWhiteSpace(Input.Notlar) ? null : Input.Notlar.Trim(),
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now
            };

            _context.SeferMasraflari.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Seferler/Details", new { id = Input.SeferID });
        }

        // Masraf tipleri artık DB'den (global MasrafTipleri lookup tablosu) okunur.
        private async Task LoadMasrafTipleriAsync()
        {
            var tipler = await _context.MasrafTipleri
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SiraNo).ThenBy(t => t.Ad)
                .Select(t => t.Ad)
                .ToListAsync();

            MasrafTipleri = new SelectList(tipler, Input.MasrafTipi);
        }
    }
}
