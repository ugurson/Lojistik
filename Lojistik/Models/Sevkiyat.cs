// Models/Sevkiyat.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("Sevkiyatlar")]
    public class Sevkiyat
    {
        [Key] public int SevkiyatID { get; set; }
        [Required] public int FirmaID { get; set; }
        [StringLength(20)] public string? SubeKodu { get; set; }
        public int? KullaniciID { get; set; }

        [Required] public int SiparisID { get; set; }
        [Required] public int AracID { get; set; }
        public int? DorseID { get; set; }
        public int? SurucuID { get; set; }

        [StringLength(100)] public string? SurucuAdi { get; set; }
        [StringLength(30)] public string? SevkiyatKodu { get; set; }

        public int? YuklemeMusteriID { get; set; }
        public int? BosaltmaMusteriID { get; set; }

        [StringLength(250)] public string? YuklemeAdres { get; set; }
        [StringLength(250)] public string? BosaltmaAdres { get; set; }
        [StringLength(200)] public string? YuklemeNoktasi { get; set; }
        [StringLength(200)] public string? BosaltmaNoktasi { get; set; }

        public DateTime? PlanlananYuklemeTarihi { get; set; } // date
        public DateTime? YuklemeTarihi { get; set; }          // datetime2(0)
        public DateTime? GumrukCikisTarihi { get; set; }      // datetime2(0)
        public DateTime? VarisTarihi { get; set; }            // datetime2(0)

        [StringLength(50)] public string? CMRNo { get; set; }
        [StringLength(50)] public string? MRN { get; set; }

        [Required] public byte Durum { get; set; }
        [StringLength(500)] public string? Notlar { get; set; }

        public int? CreatedByKullaniciID { get; set; }
        [Required] public DateTime CreatedAt { get; set; }

        // Navigations
        public Firma? Firma { get; set; }
        public Kullanici? Kullanici { get; set; }
        [ForeignKey(nameof(CreatedByKullaniciID))] public Kullanici? CreatedByKullanici { get; set; }

        public Siparis? Siparis { get; set; }
        public Arac? Arac { get; set; }
        [ForeignKey(nameof(DorseID))] public Arac? Dorse { get; set; }
        [ForeignKey(nameof(YuklemeMusteriID))] public Musteri? YuklemeMusteri { get; set; }
        [ForeignKey(nameof(BosaltmaMusteriID))] public Musteri? BosaltmaMusteri { get; set; }

        public ICollection<SeferSevkiyat> SeferBaglantilari { get; set; } = new List<SeferSevkiyat>();
    }
}
