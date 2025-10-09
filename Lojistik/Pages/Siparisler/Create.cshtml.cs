// Pages/Siparisler/Create.cshtml.cs
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lojistik.Pages.Siparisler
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? MusterilerSelect { get; set; }
        public SelectList? AraTedarikciSelect { get; set; }
        public SelectList? ParaBirimleriSelect { get; set; }

        public class InputModel
        {
            [DataType(DataType.Date)] public DateTime SiparisTarihi { get; set; } = DateTime.Today;

            [Required] public int GonderenMusteriID { get; set; }
            [Required] public int AliciMusteriID { get; set; }
            public int? AraTedarikciMusteriID { get; set; }

            public int? Adet { get; set; }
            [StringLength(50)] public string? AdetCinsi { get; set; }
            public int? Kilo { get; set; }

            [Required, StringLength(200)] public string YukAciklamasi { get; set; } = null!;
            [Column(TypeName = "decimal(18,2)")]
            public decimal? Tutar { get; set; }
            [StringLength(10)] public string? ParaBirimi { get; set; } = "TRY";

            [StringLength(50)] public string? FaturaNo { get; set; }
            [StringLength(500)] public string? Notlar { get; set; }

            [Required] public byte Durum { get; set; } = 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync();
                return Page();
            }

            var e = new Siparis
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                // SubeKodu kullanmıyoruz
                SiparisTarihi = Input.SiparisTarihi.Date,
                GonderenMusteriID = Input.GonderenMusteriID,
                AliciMusteriID = Input.AliciMusteriID,
                AraTedarikciMusteriID = Input.AraTedarikciMusteriID,

                Adet = Input.Adet,
                AdetCinsi = Input.AdetCinsi?.Trim(),
                Kilo = Input.Kilo,

                YukAciklamasi = Input.YukAciklamasi.Trim(),
                Tutar = Input.Tutar,
                ParaBirimi = string.IsNullOrWhiteSpace(Input.ParaBirimi) ? null : Input.ParaBirimi.Trim(),

                FaturaNo = Input.FaturaNo?.Trim(),
                Notlar = Input.Notlar?.Trim(),
                Durum = Input.Durum
            };

            _context.Siparisler.Add(e);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = e.SiparisID });
        }

        private async Task LoadSelectsAsync()
        {
            var firmaId = User.GetFirmaId();

            var musteriList = await _context.Musteriler
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId)
                .OrderBy(m => m.MusteriAdi)
                .Select(m => new { m.MusteriID, m.MusteriAdi })
                .ToListAsync();

            MusterilerSelect = new SelectList(musteriList, "MusteriID", "MusteriAdi");
            AraTedarikciSelect = new SelectList(musteriList, "MusteriID", "MusteriAdi");

            var pbs = new[] { "TRY", "USD", "EUR", "GBP", "CHF" };
            ParaBirimleriSelect = new SelectList(pbs.Select(x => new { Value = x, Text = x }), "Value", "Text", Input.ParaBirimi);
        }
    }
}
