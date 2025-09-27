using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("Siparisler")]
    public class Siparis
    {
        [Key] public int SiparisID { get; set; }

        // Firma/Şube/Kullanıcı
        [Required] public int FirmaID { get; set; }
        [StringLength(20)] public string? SubeKodu { get; set; }
        public int? KullaniciID { get; set; }
        public int? CreatedByKullaniciID { get; set; }

        public ICollection<Sevkiyat> Sevkiyatlar { get; set; } = new List<Sevkiyat>();

        // Müşteriler
        [Required] public int GonderenMusteriID { get; set; }
        [Required] public int AliciMusteriID { get; set; }
        public int? AraTedarikciMusteriID { get; set; }

        // İlişkiler
        public Musteri? GonderenMusteri { get; set; }
        public Musteri? AliciMusteri { get; set; }
        public Musteri? AraTedarikciMusteri { get; set; }
        // Veri
        [Required] public DateTime SiparisTarihi { get; set; } = DateTime.Today;
        [Required, StringLength(200)] public string YukAciklamasi { get; set; } = string.Empty;

        public int? Adet { get; set; }
        [StringLength(50)] public string? AdetCinsi { get; set; }
        public int? Kilo { get; set; }                 // INT (kg)

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tutar { get; set; }
        [StringLength(10)] public string? ParaBirimi { get; set; } // TL/EUR/USD
        [StringLength(50)] public string? FaturaNo { get; set; }

        public byte Durum { get; set; } = 0;           // 0=Yeni,1=Planlandı,2=Tamam,3=İptal
        [StringLength(500)] public string? Notlar { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
