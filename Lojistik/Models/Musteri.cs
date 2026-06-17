using System.ComponentModel.DataAnnotations;

namespace Lojistik.Models;

public class Musteri
{
    public int MusteriID { get; set; }

    [Required]
    public int FirmaID { get; set; }

    [Required, StringLength(200)]
    public string MusteriAdi { get; set; } = null!;

    /// <summary>0 = Müşteri  |  1 = Tedarikçi  |  2 = Nakliyeci</summary>
    public byte Kategori { get; set; } = 0;

    [Required]
    public int UlkeID { get; set; }

    [Required]
    public int SehirID { get; set; }

    [StringLength(250)]
    public string? Adres { get; set; }

    [StringLength(20)]
    public string? PostaKodu { get; set; }

    [StringLength(50)]
    public string? Telefon { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(100)]
    [Display(Name = "Vergi Dairesi")]
    public string? VergiDairesi { get; set; }

    [StringLength(50)]
    [Display(Name = "Vergi No")]
    public string? VergiNo { get; set; }

    [StringLength(150)]
    [Display(Name = "Yetkili İsim")]
    public string? YetkiliIsim { get; set; }

    public Ulke? Ulke { get; set; }
    public Sehir? Sehir { get; set; }

    [StringLength(50)]
    [Display(Name = "Gümrük Kodu")]
    public string? GumrukKod { get; set; }

    // ─── Statik yardımcılar ──────────────────────────────────────────────────
    public static string KategoriAdi(byte k) => k switch
    {
        1 => "Tedarikçi",
        2 => "Nakliyeci",
        _ => "Müşteri"
    };

    public static string KategoriBadgeCss(byte k) => k switch
    {
        1 => "bg-warning text-dark",
        2 => "bg-info text-dark",
        _ => "bg-primary bg-opacity-75"
    };
}
