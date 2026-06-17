using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.SistemAdmin.GuncellemeNotlari;

public class IndexModel : PageModel
{
    private readonly AppDbContext _ctx;
    public IndexModel(AppDbContext ctx) => _ctx = ctx;

    public List<GuncellemeNotu> Notlar { get; set; } = new();
    public string? Hata   { get; set; }
    public string? Basari { get; set; }

    public async Task OnGetAsync()
    {
        Notlar = await _ctx.GuncellemeNotlari
            .AsNoTracking()
            .OrderByDescending(n => n.Tarih)
            .ThenByDescending(n => n.GuncellemeNotuID)
            .ToListAsync();
    }

    // POST ?handler=Kaydet  (hem ekle hem güncelle)
    public async Task<IActionResult> OnPostKaydetAsync(
        int id, string baslik, string? aciklama, string? bolum,
        string? surum, DateTime tarih, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(baslik))
        {
            Hata = "Başlık zorunludur.";
            await OnGetAsync();
            return Page();
        }

        if (id == 0)
        {
            _ctx.GuncellemeNotlari.Add(new GuncellemeNotu
            {
                Baslik    = baslik.Trim(),
                Aciklama  = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                Bolum     = string.IsNullOrWhiteSpace(bolum)    ? null : bolum.Trim(),
                Surum     = string.IsNullOrWhiteSpace(surum)    ? null : surum.Trim(),
                Tarih     = tarih.Date,
                IsActive  = isActive,
                CreatedAt = DateTime.Now
            });
        }
        else
        {
            var not = await _ctx.GuncellemeNotlari.FindAsync(id);
            if (not == null) return RedirectToPage();
            not.Baslik   = baslik.Trim();
            not.Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim();
            not.Bolum    = string.IsNullOrWhiteSpace(bolum)    ? null : bolum.Trim();
            not.Surum    = string.IsNullOrWhiteSpace(surum)    ? null : surum.Trim();
            not.Tarih    = tarih.Date;
            not.IsActive = isActive;
        }

        await _ctx.SaveChangesAsync();
        TempData["StatusMessage"] = id == 0 ? "Not eklendi." : "Not güncellendi.";
        TempData["StatusType"]    = "success";
        return RedirectToPage();
    }

    // POST ?handler=Sil
    public async Task<IActionResult> OnPostSilAsync(int id)
    {
        var not = await _ctx.GuncellemeNotlari.FindAsync(id);
        if (not != null)
        {
            _ctx.GuncellemeNotlari.Remove(not);
            await _ctx.SaveChangesAsync();
            TempData["StatusMessage"] = "Not silindi.";
            TempData["StatusType"]    = "success";
        }
        return RedirectToPage();
    }

    // AJAX GET ?handler=Get&id=#
    public async Task<JsonResult> OnGetGetAsync(int id)
    {
        var not = await _ctx.GuncellemeNotlari.AsNoTracking()
            .Where(n => n.GuncellemeNotuID == id)
            .Select(n => new {
                n.GuncellemeNotuID, n.Baslik, n.Aciklama,
                n.Bolum, n.Surum, n.IsActive,
                Tarih = n.Tarih.ToString("yyyy-MM-dd")
            })
            .FirstOrDefaultAsync();
        return new JsonResult(not);
    }
}
