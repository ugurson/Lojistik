// Pages/Sevkiyatlar/Create.cshtml.cs
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

namespace Lojistik.Pages.Sevkiyatlar
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty] public InputModel Input { get; set; } = new();

        public SelectList? SiparisSelect { get; set; }
        public SelectList? CekicilerSelect { get; set; }
        public SelectList? DorselerSelect { get; set; }
        public SelectList? YuklemeMusteriSelect { get; set; }
        public SelectList? BosaltmaMusteriSelect { get; set; }

        public class InputModel
        {
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

        public async Task<IActionResult> OnGetAsync(int? siparisId = null)
        {
            await LoadSelectsAsync(siparisId, null, null, null);
            if (siparisId.HasValue) Input.SiparisID = siparisId.Value;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(Input.SiparisID, Input.AracID, Input.DorseID, Input.YuklemeMusteriID);
                return Page();
            }

            var e = new Sevkiyat
            {
                FirmaID = firmaId,
                KullaniciID = userId,
                CreatedByKullaniciID = userId,
                CreatedAt = DateTime.Now,

                SubeKodu = null,
                SiparisID = Input.SiparisID,
                AracID = Input.AracID,
                DorseID = Input.DorseID,
                SurucuID = Input.SurucuID,
                SurucuAdi = Input.SurucuAdi?.Trim(),
                SevkiyatKodu = Input.SevkiyatKodu?.Trim(),
                YuklemeMusteriID = Input.YuklemeMusteriID,
                BosaltmaMusteriID = Input.BosaltmaMusteriID,
                YuklemeAdres = Input.YuklemeAdres?.Trim(),
                BosaltmaAdres = Input.BosaltmaAdres?.Trim(),
                YuklemeNoktasi = Input.YuklemeNoktasi?.Trim(),
                BosaltmaNoktasi = Input.BosaltmaNoktasi?.Trim(),
                PlanlananYuklemeTarihi = Input.PlanlananYuklemeTarihi?.Date,
                YuklemeTarihi = Input.YuklemeTarihi,
                GumrukCikisTarihi = Input.GumrukCikisTarihi,
                VarisTarihi = Input.VarisTarihi,
                CMRNo = Input.CMRNo?.Trim(),
                MRN = Input.MRN?.Trim(),
                Durum = Input.Durum,
                Notlar = Input.Notlar?.Trim()
            };

            _context.Sevkiyatlar.Add(e);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = e.SevkiyatID });
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
