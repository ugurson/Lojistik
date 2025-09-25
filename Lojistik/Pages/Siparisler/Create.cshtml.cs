// Pages/Siparisler/Create.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? MusterilerSelect { get; set; }

        public class InputModel
        {
            [Required, DataType(DataType.Date)]
            public DateTime SiparisTarihi { get; set; } = DateTime.Today;

            [Required] public int GonderenMusteriID { get; set; }
            [Required] public int AliciMusteriID { get; set; }
            public int? AraTedarikciMusteriID { get; set; }

            [Required, StringLength(200)]
            public string YukAciklamasi { get; set; } = null!;

            public int? Adet { get; set; }
            [StringLength(50)] public string? AdetCinsi { get; set; }
            public int? Kilo { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? Tutar { get; set; }

            [StringLength(10)] public string? ParaBirimi { get; set; } = "TRY";
            [StringLength(50)] public string? FaturaNo { get; set; }
            [StringLength(20)] public string? SubeKodu { get; set; }
            [StringLength(500)] public string? Notlar { get; set; }

            [Required] public byte Durum { get; set; } = 0;
        }

        public async Task<IActionResult> OnGetAsync(int? gonderenId = null, int? aliciId = null)
        {
            await LoadMusterilerAsync();
            if (gonderenId.HasValue) Input.GonderenMusteriID = gonderenId.Value;
            if (aliciId.HasValue) Input.AliciMusteriID = aliciId.Value;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadMusterilerAsync();
                return Page();
            }

            var e = new Siparis
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                SubeKodu = Input.SubeKodu?.Trim(),
                SiparisTarihi = Input.SiparisTarihi.Date,
                GonderenMusteriID = Input.GonderenMusteriID,
                AliciMusteriID = Input.AliciMusteriID,
                AraTedarikciMusteriID = Input.AraTedarikciMusteriID,

                YukAciklamasi = Input.YukAciklamasi.Trim(),
                Adet = Input.Adet,
                AdetCinsi = Input.AdetCinsi?.Trim(),
                Kilo = Input.Kilo,
                Tutar = Input.Tutar,
                ParaBirimi = string.IsNullOrWhiteSpace(Input.ParaBirimi) ? null : Input.ParaBirimi!.Trim(),
                FaturaNo = Input.FaturaNo?.Trim(),
                Notlar = Input.Notlar?.Trim(),
                Durum = Input.Durum
            };

            _context.Siparisler.Add(e);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = e.SiparisID });
        }

        private async Task LoadMusterilerAsync()
        {
            var firmaId = User.GetFirmaId();

            MusterilerSelect = new SelectList(
                await _context.Musteriler
                    .AsNoTracking()
                    .Where(m => m.FirmaID == firmaId)
                    .OrderBy(m => m.MusteriAdi)
                    .Select(m => new { m.MusteriID, m.MusteriAdi })
                    .ToListAsync(),
                "MusteriID", "MusteriAdi"
            );
        }
    }
}
