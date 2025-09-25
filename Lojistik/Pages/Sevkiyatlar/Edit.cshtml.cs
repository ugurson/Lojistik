// Pages/Sevkiyatlar/Edit.cshtml.cs
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

namespace Lojistik.Pages.Sevkiyatlar
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? SiparisSelect { get; set; }
        public SelectList? CekicilerSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }
        public SelectList? YuklemeMusteriSelect { get; set; }
        public SelectList? BosaltmaMusteriSelect { get; set; }

        public class InputModel
        {
            [Required] public int SevkiyatID { get; set; }
            [Required] public int SiparisID { get; set; }

            [Required] public int AracID { get; set; }
            public int? DorseID { get; set; }

            public int? SurucuID { get; set; }
            [StringLength(100)] public string? SurucuAdi { get; set; }

            [StringLength(30)] public string? SevkiyatKodu { get; set; }

            public int? YuklemeMusteriID { get; set; }
            public int? BosaltmaMusteriID { get; set; }

            [StringLength(250)] public string? YuklemeAdres { get; set; }
            [StringLength(250)] public string? BosaltmaAdres { get; set; }
            [StringLength(200)] public string? YuklemeNoktasi { get; set; }
            [StringLength(200)] public string? BosaltmaNoktasi { get; set; }

            [DataType(DataType.Date)] public DateTime? PlanlananYuklemeTarihi { get; set; }
            public DateTime? YuklemeTarihi { get; set; }
            public DateTime? GumrukCikisTarihi { get; set; }
            public DateTime? VarisTarihi { get; set; }

            [StringLength(50)] public string? CMRNo { get; set; }
            [StringLength(50)] public string? MRN { get; set; }

            [Required] public byte Durum { get; set; } = 0;
            [StringLength(500)] public string? Notlar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var firmaId = User.GetFirmaId();

            var s = await _context.Sevkiyatlar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == id);

            if (s == null) return RedirectToPage("./Index");

            Input = new InputModel
            {
                SevkiyatID = s.SevkiyatID,
                SiparisID = s.SiparisID,
                AracID = s.AracID,
                DorseID = s.DorseID,
                SurucuID = s.SurucuID,
                SurucuAdi = s.SurucuAdi,
                SevkiyatKodu = s.SevkiyatKodu,
                YuklemeMusteriID = s.YuklemeMusteriID,
                BosaltmaMusteriID = s.BosaltmaMusteriID,
                YuklemeAdres = s.YuklemeAdres,
                BosaltmaAdres = s.BosaltmaAdres,
                YuklemeNoktasi = s.YuklemeNoktasi,
                BosaltmaNoktasi = s.BosaltmaNoktasi,
                PlanlananYuklemeTarihi = s.PlanlananYuklemeTarihi,
                YuklemeTarihi = s.YuklemeTarihi,
                GumrukCikisTarihi = s.GumrukCikisTarihi,
                VarisTarihi = s.VarisTarihi,
                CMRNo = s.CMRNo,
                MRN = s.MRN,
                Durum = s.Durum,
                Notlar = s.Notlar
            };

            await LoadSelectsAsync(Input.SiparisID, Input.AracID, Input.DorseID, Input.YuklemeMusteriID);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SiparisID, Input.AracID, Input.DorseID, Input.YuklemeMusteriID);
                return Page();
            }

            var s = await _context.Sevkiyatlar
                .FirstOrDefaultAsync(x => x.FirmaID == firmaId && x.SevkiyatID == Input.SevkiyatID);

            if (s == null) return RedirectToPage("./Index");

            s.SiparisID = Input.SiparisID;
            s.AracID = Input.AracID;
            s.DorseID = Input.DorseID;
            s.SurucuID = Input.SurucuID;
            s.SurucuAdi = Input.SurucuAdi?.Trim();
            s.SevkiyatKodu = Input.SevkiyatKodu?.Trim();
            s.YuklemeMusteriID = Input.YuklemeMusteriID;
            s.BosaltmaMusteriID = Input.BosaltmaMusteriID;
            s.YuklemeAdres = Input.YuklemeAdres?.Trim();
            s.BosaltmaAdres = Input.BosaltmaAdres?.Trim();
            s.YuklemeNoktasi = Input.YuklemeNoktasi?.Trim();
            s.BosaltmaNoktasi = Input.BosaltmaNoktasi?.Trim();
            s.PlanlananYuklemeTarihi = Input.PlanlananYuklemeTarihi?.Date;
            s.YuklemeTarihi = Input.YuklemeTarihi;
            s.GumrukCikisTarihi = Input.GumrukCikisTarihi;
            s.VarisTarihi = Input.VarisTarihi;
            s.CMRNo = Input.CMRNo?.Trim();
            s.MRN = Input.MRN?.Trim();
            s.Durum = Input.Durum;
            s.Notlar = Input.Notlar?.Trim();

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = s.SevkiyatID });
        }

        private async Task LoadSelectsAsync(int? siparisId, int? cekiciId, int? dorseId, int? yuklemeMusteriId)
        {
            var firmaId = User.GetFirmaId();

            SiparisSelect = new SelectList(
                await _context.Siparisler
                    .AsNoTracking()
                    .Where(x => x.FirmaID == firmaId)
                    .OrderByDescending(x => x.SiparisID)
                    .Select(x => new { x.SiparisID, Text = x.SiparisID + " - " + x.YukAciklamasi })
                    .ToListAsync(),
                "SiparisID", "Text", siparisId
            );

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

            YuklemeMusteriSelect = new SelectList(
                await _context.Musteriler
                    .AsNoTracking()
                    .Where(m => m.FirmaID == firmaId)
                    .OrderBy(m => m.MusteriAdi)
                    .Select(m => new { m.MusteriID, m.MusteriAdi })
                    .ToListAsync(),
                "MusteriID", "MusteriAdi", yuklemeMusteriId
            );
            BosaltmaMusteriSelect = YuklemeMusteriSelect;
        }
    }
}
