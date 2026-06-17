using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Models;

[Table("ProgramFirmalar")]
public class ProgramFirma
{
    [Key]
    public int FirmaID { get; set; }

    [Required, StringLength(20)]
    public string FirmaKodu { get; set; } = null!;

    [Required, StringLength(200)]
    public string FirmaAdi { get; set; } = null!;

    public DateOnly BaslamaTarihi { get; set; }
    public DateOnly? BitisTarihi { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notlar { get; set; }

    public int KullaniciLimiti { get; set; } = 5;

    public decimal? AylikUcret { get; set; }

    [StringLength(10)]
    public string ParaBirimi { get; set; } = "TL";

    [StringLength(100)]
    public string? PaketAdi { get; set; }

    public bool DemoMu { get; set; } = false;
    public int? DemoKayitLimiti { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
