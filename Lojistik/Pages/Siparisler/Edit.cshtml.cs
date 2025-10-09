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
        public SelectList? AraTedarikciSelect { get; set; }
        public SelectList? ParaBirimleriSelect { get; set; }

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

            // Yenisi (decimal için doğru Range)
            // InputModel içinde:
            [Range(typeof(decimal), "0", "9999999999999,99", ErrorMessage = "Geçersiz tutar.")]
            public decimal? Tutar { get; set; }

            [StringLength(10)] public string? ParaBirimi { get; set; } // PB combobox
            [StringLength(50)] public string? FaturaNo { get; set; }
            // ŞubeKodu KALDIRILDI
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
                Notlar = s.Notlar,
                Durum = s.Durum
            };

            await LoadSelectsAsync(
                selectedGonderenId: s.GonderenMusteriID,
                selectedAliciId: s.AliciMusteriID,
                selectedAraId: s.AraTedarikciMusteriID,
                selectedPB: s.ParaBirimi
            );

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(
                    selectedGonderenId: Input.GonderenMusteriID,
                    selectedAliciId: Input.AliciMusteriID,
                    selectedAraId: Input.AraTedarikciMusteriID,
                    selectedPB: Input.ParaBirimi
                );
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
            s.Notlar = Input.Notlar?.Trim();
            s.Durum = Input.Durum;

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = s.SiparisID });
        }

        private async Task LoadSelectsAsync(int? selectedGonderenId, int? selectedAliciId, int? selectedAraId, string? selectedPB)
        {
            var firmaId = User.GetFirmaId();

            var musteriList = await _context.Musteriler
                .AsNoTracking()
                .Where(m => m.FirmaID == firmaId)
                .OrderBy(m => m.MusteriAdi)
                .Select(m => new { m.MusteriID, m.MusteriAdi })
                .ToListAsync();

            MusterilerSelect = new SelectList(musteriList, "MusteriID", "MusteriAdi", selectedGonderenId);
            // Edit.cshtml tarafında Gönderen ve Alıcı ikisi de MusterilerSelect'i kullanıyor (ayrı selected değerini Razor belirler)
            AraTedarikciSelect = new SelectList(musteriList, "MusteriID", "MusteriAdi", selectedAraId);

            // Para birimleri – Create ile aynı set
            var pb = new[]
            {
                new { Value = "TL",  Text = "TL - Türk Lirası" },
                new { Value = "EUR", Text = "EUR - Euro" },
                new { Value = "USD", Text = "USD - Amerikan Doları" }
            };
            ParaBirimleriSelect = new SelectList(pb, "Value", "Text", selectedPB);
        }
    }
}
