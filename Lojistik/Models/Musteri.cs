using System.ComponentModel.DataAnnotations;

namespace Lojistik.Models;

public class Musteri
{
    public int MusteriID { get; set; }

    [Required]
    public int FirmaID { get; set; }

    [Required, StringLength(200)]
    public string MusteriAdi { get; set; } = null!;

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

    public Ulke? Ulke { get; set; }
    public Sehir? Sehir { get; set; }

    [StringLength(50)]
    [Display(Name = "Gümrük Kodu")]
    public string? GumrukKod { get; set; }
}
