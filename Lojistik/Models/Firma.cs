using System.ComponentModel.DataAnnotations;

namespace Lojistik.Models;

public class Firma
{
    public int FirmaID { get; set; }
    public string FirmaKodu { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string FirmaAdi { get; set; } = null!;
    public string? LogoYolu { get; set; }
    public string? LogoDosyaAdi { get; set; }
    public string? LogoMimeType { get; set; }

    // ── Lisans alanları (Faz 1'de Firmalar'a eklendi + ProgramFirmalar'dan backfill edildi) ──
    // Tümü DB'de nullable; ProgramFirmalar'daki karşılıklarıyla tip uyumlu.
    public DateOnly? BaslamaTarihi { get; set; }
    public DateOnly? BitisTarihi { get; set; }
    public int? KullaniciLimiti { get; set; }
    public decimal? AylikUcret { get; set; }

    [StringLength(10)]
    public string? ParaBirimi { get; set; }

    [StringLength(100)]
    public string? PaketAdi { get; set; }

    public bool? DemoMu { get; set; }
    public int? DemoKayitLimiti { get; set; }

    [StringLength(500)]
    public string? Notlar { get; set; }

    public ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
}