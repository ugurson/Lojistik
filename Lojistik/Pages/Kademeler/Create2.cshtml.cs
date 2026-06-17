using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Kademeler;

[Authorize]
public class Create2Model : PageModel
{
    private readonly AppDbContext _db;
    public Create2Model(AppDbContext db) => _db = db;

    public List<SelectListItem> Araclar { get; set; } = new();
    public List<SelectListItem> KademeFirmalari { get; set; } = new();

    [BindProperty] public string? SeciliFirma { get; set; }  // dropdown se�imi
    [BindProperty] public string? FirmaElle { get; set; }    // elle giri�

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Ara� se�iniz.")]
        public int? AracID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Tarih { get; set; } = DateTime.Today;

        [Required, StringLength(200)]
        public string YapilanIslem { get; set; } = "";

        [Range(0, 999999999)]
        public decimal Tutar { get; set; }

        [Required, StringLength(10)]
        public string ParaBirimi { get; set; } = "TRY";

        public string? Notlar { get; set; }
        public string? IslemFirmasi { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadAraclarAsync();
        await LoadKademeFirmalariAsync();

    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAraclarAsync();
        await LoadKademeFirmalariAsync();

        var unvan = (SeciliFirma == "__ELLE__" ? FirmaElle : SeciliFirma)?.Trim();

        if (string.IsNullOrWhiteSpace(unvan))
        {
            ModelState.AddModelError("", "Yap�lan Firma se�iniz veya elle giriniz.");
            return Page();
        }

        Input.IslemFirmasi = unvan;

        if (!ModelState.IsValid)
            return Page();

        var firmaId = GetFirmaId();

        // g�venlik: ara� se�imi kendi firmas�na m�?
        var aracOk = await _db.Araclar.AsNoTracking()
            .AnyAsync(a => a.AracID == Input.AracID && a.FirmaID == firmaId);

        if (!aracOk)
        {
            ModelState.AddModelError("", "Se�ilen ara� bulunamad�.");
            return Page();
        }

        var entity = new AracKademe
        {
            AracID = Input.AracID!.Value,
            Tarih = Input.Tarih.Date,
            YapilanIslem = Input.YapilanIslem.Trim(),
            Tutar = Input.Tutar,
            ParaBirimi = Input.ParaBirimi,
            Notlar = string.IsNullOrWhiteSpace(Input.Notlar) ? null : Input.Notlar.Trim(),
            IslemFirmasi =Input.IslemFirmasi?.Trim()
        };

        _db.AracKademeler.Add(entity);
        await _db.SaveChangesAsync();

        // istersen Index�e ara� filtresiyle d�n
        return RedirectToPage("./Index", new { aracId = entity.AracID });
    }

    private async Task LoadAraclarAsync()
    {
        var firmaId = GetFirmaId();
        Araclar = await _db.Araclar.AsNoTracking()
            .Where(a => a.FirmaID == firmaId)
            .OrderBy(a => a.Plaka)
            .Select(a => new SelectListItem { Value = a.AracID.ToString(), Text = a.Plaka })
            .ToListAsync();
    }

    private int GetFirmaId()
    {
        var v = User.FindFirstValue("FirmaID") ?? User.FindFirstValue("FirmaId");
        if (int.TryParse(v, out var firmaId)) return firmaId;
        throw new InvalidOperationException("FirmaID claim bulunamad�.");
    }

    private async Task LoadKademeFirmalariAsync()
    {
        var firmaId = GetFirmaId();

        // KademeFirmalari tablosu kaldırıldı — artık Musteriler (Kategori=1 · Tedarikçi) kullanılıyor
        KademeFirmalari = await _db.Musteriler.AsNoTracking()
            .Where(m => m.FirmaID == firmaId && m.Kategori == 1 && m.IsActive)
            .OrderBy(m => m.MusteriAdi)
            .Select(m => new SelectListItem { Value = m.MusteriAdi, Text = m.MusteriAdi })
            .ToListAsync();

        KademeFirmalari.Insert(0, new SelectListItem { Value = "",         Text = "Seçiniz..." });
        KademeFirmalari.Add   (   new SelectListItem { Value = "__ELLE__", Text = "Diğer (Elle gir)" });
    }

}
