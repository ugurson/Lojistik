// Models/Sefer.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("Seferler")]
    public class Sefer
    {
        [Key] public int SeferID { get; set; }
        [Required] public int FirmaID { get; set; }
        [StringLength(20)] public string? SubeKodu { get; set; }
        public int? KullaniciID { get; set; }

        [Required] public int AracID { get; set; }
        public int? DorseID { get; set; }
        public int? SurucuID { get; set; }

        [StringLength(100)] public string? SurucuAdi { get; set; }
        [StringLength(30)] public string? SeferKodu { get; set; }

        public DateTime? CikisTarihi { get; set; } // datetime2(0)
        public DateTime? DonusTarihi { get; set; } // datetime2(0)

        [Required] public byte Durum { get; set; }
        [StringLength(500)] public string? Notlar { get; set; }

        public int? CreatedByKullaniciID { get; set; }
        [Required] public DateTime CreatedAt { get; set; }

        // Navigations
        public Firma? Firma { get; set; }
        public Kullanici? Kullanici { get; set; }
        [ForeignKey(nameof(CreatedByKullaniciID))] public Kullanici? CreatedByKullanici { get; set; }

        public Arac? Arac { get; set; }
        [ForeignKey(nameof(DorseID))] public Arac? Dorse { get; set; }

        public ICollection<SeferSevkiyat> SeferSevkiyatlar { get; set; } = new List<SeferSevkiyat>();
        public ICollection<SeferMasraf> SeferMasraflari { get; set; } = new List<SeferMasraf>();
        public ICollection<SeferGelir> SeferGelirleri { get; set; } = new List<SeferGelir>();
    }
}
