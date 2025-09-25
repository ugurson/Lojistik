// Pages/Siparisler/Edit.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Siparisler
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? MusterilerSelect { get; set; }

        public class InputModel
        {
            [Required] public int SiparisID { get; set; }
            [Required, DataType(DataType.Date)] public DateTime SiparisTarihi { get; set; }

            [Required] public int GonderenMusteriID { get; set; }
            [Required] public int AliciMusteriID { get; set; }
            public int? AraTedarikciMusteriID { get; set; }

            [Required, StringLength(200)] public string YukAciklamasi { get; set; } = null!;
            public int? Adet { get; set; }
            [StringLength(50)] public string? AdetCinsi { get; set; }
            public int? Kilo { get; set; }

            [Range(0, double.MaxValue)] public decimal? Tutar { get; set; }
            [StringLength(10)] public string? ParaBirimi { get; set; }
            [StringLength(50)] public string? FaturaNo { get; set; }
            [StringLength(20)] public string? SubeKodu { get; set; }
            [StringLength(500)] public string? Notlar { get; set; }
            [Required] public byte Durum { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var s = await _context.Siparisler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SiparisID == id);

            if (s == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SiparisID = s.SiparisID,
                SiparisTarihi = s.SiparisTarihi,
                GonderenMusteriID = s.GonderenMusteriID,
                AliciMusteriID = s.AliciMusteriID,
                AraTedarikciMusteriID = s.AraTedarikciMusteriID,
                YukAciklamasi = s.YukAciklamasi,
                Adet = s.Adet,
                AdetCinsi = s.AdetCinsi,
                Kilo = s.Kilo,
                Tutar = s.Tutar,
                ParaBirimi = s.ParaBirimi,
                FaturaNo = s.FaturaNo,
                SubeKodu = s.SubeKodu,
                Notlar = s.Notlar,
                Durum = s.Durum
            };

            await LoadMusterilerAsync(s.GonderenMusteriID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadMusterilerAsync(Input.GonderenMusteriID);
                return Page();
            }

            var s = await _context.Siparisler
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SiparisID == Input.SiparisID);

            if (s == null) return RedirectToPage("./Index");

            s.SiparisTarihi = Input.SiparisTarihi.Date;
            s.GonderenMusteriID = Input.GonderenMusteriID;
            s.AliciMusteriID = Input.AliciMusteriID;
            s.AraTedarikciMusteriID = Input.AraTedarikciMusteriID;
            s.YukAciklamasi = Input.YukAciklamasi.Trim();
            s.Adet = Input.Adet;
            s.AdetCinsi = Input.AdetCinsi?.Trim();
            s.Kilo = Input.Kilo;
            s.Tutar = Input.Tutar;
            s.ParaBirimi = string.IsNullOrWhiteSpace(Input.ParaBirimi) ? null : Input.ParaBirimi!.Trim();
            s.FaturaNo = Input.FaturaNo?.Trim();
            s.SubeKodu = Input.SubeKodu?.Trim();
            s.Notlar = Input.Notlar?.Trim();
            s.Durum = Input.Durum;

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = s.SiparisID });
        }

        private async Task LoadMusterilerAsync(int? selectedId)
        {
            var firmaId = User.GetFirmaId();
            MusterilerSelect = new SelectList(
                await _context.Musteriler
                    .AsNoTracking()
                    .Where(m => m.FirmaID == firmaId)
                    .OrderBy(m => m.MusteriAdi)
                    .Select(m => new { m.MusteriID, m.MusteriAdi })
                    .ToListAsync(),
                "MusteriID", "MusteriAdi", selectedId
            );
        }
    }
}
