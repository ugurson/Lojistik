// Models/SeferGelir.cs
using Lojistik.Models;
using System.ComponentModel.DataAnnotations;

public class SeferGelir
{
    public int SeferGelirID { get; set; }
    public int FirmaID { get; set; }
    public string? SubeKodu { get; set; }
    public int? KullaniciID { get; set; }
    public int SeferID { get; set; }
    [DataType(DataType.Date)] public DateTime Tarih { get; set; } = DateTime.Today;

    [StringLength(200)] public string? Aciklama { get; set; }

    [Range(typeof(decimal), "0", "9999999999999,99")]
    public decimal Tutar { get; set; }

    [Required, StringLength(10)] public string ParaBirimi { get; set; } = "TL";
    public int? IlgiliSiparisID { get; set; }
    [StringLength(300)] public string? Notlar { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Sefer? Sefer { get; set; }
    public Siparis? IlgiliSiparis { get; set; }
}
