// Models/SeferGelir.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("SeferGelirleri")]
    public class SeferGelir
    {
        [Key] public int SeferGelirID { get; set; }
        [Required] public int FirmaID { get; set; }
        [StringLength(20)] public string? SubeKodu { get; set; }
        public int? KullaniciID { get; set; }

        [Required] public int SeferID { get; set; }
        [Required] public DateTime Tarih { get; set; } // date

        [StringLength(200)] public string? Aciklama { get; set; }
        [Required] public decimal Tutar { get; set; }
        [Required, StringLength(10)] public string ParaBirimi { get; set; } = null!;

        public int? IlgiliSiparisID { get; set; }
        [StringLength(300)] public string? Notlar { get; set; }

        [Required] public DateTime CreatedAt { get; set; }

        // Navigations
        public Firma? Firma { get; set; }
        public Kullanici? Kullanici { get; set; }
        public Sefer? Sefer { get; set; }
        [ForeignKey(nameof(IlgiliSiparisID))] public Siparis? IlgiliSiparis { get; set; }
    }
}
