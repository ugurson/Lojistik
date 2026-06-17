// Pages/Seferler/Edit.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lojistik.Pages.Seferler
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();
        public SelectList? CekicilerSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }

        public class InputModel
        {
            [Required] public int SeferID { get; set; }

            [StringLength(30)] public string? SeferKodu { get; set; }

            [Required] public int AracID { get; set; }
            public int? DorseID { get; set; }

            public int? SurucuID { get; set; }
            [StringLength(100)] public string? SurucuAdi { get; set; }

            public DateTime? CikisTarihi { get; set; }
            public DateTime? DonusTarihi { get; set; }

            [Required] public byte Durum { get; set; } = 0;
            [StringLength(500)] public string? Notlar { get; set; }
            public int? BaslangicKm { get; set; }
            public int? BitisKm { get; set; }
            public int? KmMesafe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var s = await _context.Seferler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SeferID == id);

            if (s == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SeferID = s.SeferID,
                SeferKodu = s.SeferKodu,
                AracID = s.AracID,
                DorseID = s.DorseID,
                SurucuID = s.SurucuID,
                SurucuAdi = s.SurucuAdi,
                CikisTarihi = s.CikisTarihi,
                DonusTarihi = s.DonusTarihi,
                Durum = s.Durum,
                Notlar = s.Notlar,
                BaslangicKm = s.BaslangicKm,
                BitisKm = s.BitisKm,
                KmMesafe = s.KmMesafe
            };

            await LoadSelectsAsync(Input.AracID, Input.DorseID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.AracID, Input.DorseID);
                return Page();
            }

            var s = await _context.Seferler
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SeferID == Input.SeferID);

            if (s == null) return RedirectToPage("./Index");

            // AracID zorunlu FK — firma kontrolü
            var aracAit = await _context.Araclar
                .AnyAsync(a => a.FirmaID == firmaId && a.AracID == Input.AracID);
            if (!aracAit) return Forbid();

            // DorseID opsiyonel FK — null değilse firma kontrolü
            if (Input.DorseID is > 0)
            {
                var dorseAit = await _context.Araclar
                    .AnyAsync(a => a.FirmaID == firmaId && a.AracID == Input.DorseID.Value);
                if (!dorseAit) return Forbid();
            }

            s.SeferKodu = Input.SeferKodu?.Trim();
            s.AracID = Input.AracID;
            s.DorseID = Input.DorseID;
            s.SurucuID = Input.SurucuID;
            s.SurucuAdi = Input.SurucuAdi?.Trim();
            s.CikisTarihi = Input.CikisTarihi;
            s.DonusTarihi = Input.DonusTarihi;
            s.Durum = Input.Durum;
            s.Notlar = Input.Notlar?.Trim();
            s.BaslangicKm = Input.BaslangicKm;
            s.BitisKm = Input.BitisKm;
            // KmMesafe: BitisKm - BaslangicKm otomatik, yoksa manuel değer
            s.KmMesafe = (Input.BitisKm.HasValue && Input.BaslangicKm.HasValue && Input.BitisKm > Input.BaslangicKm)
                ? Input.BitisKm.Value - Input.BaslangicKm.Value
                : Input.KmMesafe;

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = s.SeferID });
        }

        private async Task LoadSelectsAsync(int? cekiciId, int? dorseId)
        {
            var firmaId = User.GetFirmaId();

            CekicilerSelect = new SelectList(
                await _context.Araclar
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && (a.IsDorse == false || a.IsDorse == null))
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", cekiciId
            );

            DorselerSelect = new SelectList(
                await _context.Araclar
                    .AsNoTracking()
                    .Where(a => a.FirmaID == firmaId && a.IsDorse == true)
                    .OrderBy(a => a.Plaka)
                    .Select(a => new { a.AracID, a.Plaka })
                    .ToListAsync(),
                "AracID", "Plaka", dorseId
            );
        }
    }
}
