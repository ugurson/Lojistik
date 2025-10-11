using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId(), GetUserId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lojistik.Pages.Cari
{
    public class ManuelIslemModel : PageModel
    {
        private readonly AppDbContext _context;
        public ManuelIslemModel(AppDbContext context) => _context = context;

        public List<SelectListItem> MusteriOptions { get; set; } = new();

        // (opsiyonel) seçili müşterinin güncel neti (PB bazında)
        public decimal? GuncelNetPB { get; set; }

        [BindProperty] public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int MusteriID { get; set; }
            public string ParaBirimi { get; set; } = "TL";
            public DateTime Tarih { get; set; } = DateTime.Today;

            // İmzalı tutar: + alacak, - borç/kesinti
            public decimal TutarSigned { get; set; }

            public decimal? Kur { get; set; }              // opsiyonel
            public string? Kategori { get; set; }          // örn: Kesinti, İade, Düzeltme, Manuel
            public string? EvrakNo { get; set; }           // opsiyonel
            public string? Aciklama { get; set; }          // opsiyonel
            public string? SubeKodu { get; set; }          // opsiyonel
        }

        public async Task<IActionResult> OnGetAsync(int? musteriId, string? pb)
        {
            var firmaId = User.GetFirmaId();

            // Müşteri dropdown
            MusteriOptions = await _context.Musteriler.AsNoTracking()
                .Where(m => m.FirmaID == firmaId)
                .OrderBy(m => m.MusteriAdi)
                .Select(m => new SelectListItem { Value = m.MusteriID.ToString(), Text = m.MusteriAdi })
                .ToListAsync();

            if (musteriId.HasValue && musteriId.Value > 0)
                Input.MusteriID = musteriId.Value;

            if (!string.IsNullOrWhiteSpace(pb))
                Input.ParaBirimi = pb.Trim().ToUpperInvariant();

            // seçili müşteri + PB için (opsiyonel) güncel net
            if (Input.MusteriID > 0 && !string.IsNullOrWhiteSpace(Input.ParaBirimi))
            {
                var net = await _context.CariHareketler.AsNoTracking()
                    .Where(ch => ch.FirmaID == firmaId
                              && ch.MusteriID == Input.MusteriID
                              && ch.ParaBirimi == Input.ParaBirimi)
                    .Select(ch => ch.Yonu == 1 ? ch.Tutar : -ch.Tutar)
                    .SumAsync();
                GuncelNetPB = net;
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            var firmaId = User.GetFirmaId();
            var userId = User.GetUserId();

            // Basit doğrulamalar
            if (Input.MusteriID <= 0)
                ModelState.AddModelError(nameof(Input.MusteriID), "Müşteri seçiniz.");

            if (string.IsNullOrWhiteSpace(Input.ParaBirimi))
                ModelState.AddModelError(nameof(Input.ParaBirimi), "Para birimi zorunlu.");

            if (Input.TutarSigned == 0)
                ModelState.AddModelError(nameof(Input.TutarSigned), "Tutar sıfır olamaz.");

            if (!ModelState.IsValid)
            {
                // dropdown’ı yeniden doldur
                await OnGetAsync(Input.MusteriID, Input.ParaBirimi);
                return Page();
            }

            var pb = Input.ParaBirimi.Trim().ToUpperInvariant();
            var yon = Input.TutarSigned > 0 ? 1 : 0;
            var tutarAbs = Math.Abs(Input.TutarSigned);

            // SubeKodu & EvrakNo boşsa NULL, Kur opsiyonel (NULL olabilir)
            object? subeParam = string.IsNullOrWhiteSpace(Input.SubeKodu) ? null : Input.SubeKodu;
            object? evrakParam = string.IsNullOrWhiteSpace(Input.EvrakNo) ? null : Input.EvrakNo;
            object? kurParam = Input.Kur;

            var kategori = string.IsNullOrWhiteSpace(Input.Kategori) ? "Manuel" : Input.Kategori.Trim();
            var aciklama = string.IsNullOrWhiteSpace(Input.Aciklama)
                ? $"Manuel işlem ({kategori})"
                : Input.Aciklama!.Trim();

            var sql = @"
INSERT INTO dbo.CariHareketler
(
  FirmaID, SubeKodu, KullaniciID, MusteriID,
  IlgiliSiparisID, IlgiliSevkiyatID,
  Tarih,
  IslemTuru, EvrakNo, Aciklama,
  ParaBirimi, Yonu, Tutar, Kur, CreatedByKullaniciID
)
VALUES
(
  {0}, {1}, {2}, {3},
  NULL, NULL,
  {4},
  N'Manuel', {5}, {6},
  {7}, {8}, {9}, {10}, {11}
);";

            try
            {
                await _context.Database.ExecuteSqlRawAsync(sql, new object?[]
                {
                    firmaId,               // 0
                    subeParam,             // 1
                    userId,                // 2
                    Input.MusteriID,       // 3
                    Input.Tarih.Date,      // 4
                    evrakParam,            // 5
                    aciklama,              // 6
                    pb,                    // 7
                    yon,                   // 8  (1=Alacak, 0=Borç)
                    tutarAbs,              // 9
                    kurParam,              // 10 (NULL olabilir)
                    userId                 // 11
                });

                TempData["StatusMessage"] = "Manuel işlem eklendi.";
                // Kayıt sonrası aynı sayfada kalıp temiz form gösterelim:
                return RedirectToPage(new { musteriId = Input.MusteriID, pb = pb });
                // Alternatif: Ekstre'ye yönlendirmek için:
                // return RedirectToPage("/Cari/Ekstre", new { musteriId = Input.MusteriID, pb = pb });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Kayıt sırasında hata: " + (ex.InnerException?.Message ?? ex.Message));
                await OnGetAsync(Input.MusteriID, Input.ParaBirimi);
                return Page();
            }
        }
    }
}
