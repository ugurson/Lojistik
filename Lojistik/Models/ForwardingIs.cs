using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("ForwardingIsler")]
    public class ForwardingIs
    {
        [Key] public int ForwardingID { get; set; }
        [Required] public int FirmaID { get; set; }

        [StringLength(30)] public string? DosyaNo { get; set; }

        // Eski tek-müşteri alanı (opsiyonel, geriye uyumluluk)
        public int? MusteriID { get; set; }

        // ─── 3 Taraf ───────────────────────────────────────────────────────────
        public int? YukuVerenID { get; set; }       // Shipper / Consignor
        public int? YukuAlanID { get; set; }         // Consignee / Receiver
        public int? TasiyiciMusteriID { get; set; }  // Carrier (sistemdeki firma)

        // Sistemde kayıtlı olmayan taşıyıcılar için serbest metin
        [StringLength(150)] public string? TasiyiciAdi { get; set; }
        [StringLength(100)] public string? TasiyiciUlke { get; set; }

        // ─── Tarihler & Güzergah ───────────────────────────────────────────────
        public DateTime? YuklemeTarihi { get; set; }
        public DateTime? TeslimTarihi { get; set; }
        public DateTime? GercekTeslimTarihi { get; set; }

        [StringLength(300)] public string? YuklemeyeriAdi { get; set; }
        [StringLength(300)] public string? VarisYeriAdi { get; set; }

        // ─── Yük ──────────────────────────────────────────────────────────────
        [StringLength(200)] public string? EsyaCinsi { get; set; }
        [Column(TypeName = "decimal(10,2)")] public decimal? BrutKg { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? CbmHacim { get; set; }

        // 0=Taslak 1=Aktif 2=Yüklendi 3=Teslim Edildi 4=İptal
        public byte Durum { get; set; } = 0;

        // ─── Evraklar ─────────────────────────────────────────────────────────
        [StringLength(50)] public string? CmrNo { get; set; }
        [StringLength(50)] public string? TransitNo { get; set; }
        [StringLength(50)] public string? GumrukBeyanNo { get; set; }

        public string? Notlar { get; set; }

        public int? CreatedByKullaniciID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ─── Navigation Properties ─────────────────────────────────────────────
        public Musteri? Musteri { get; set; }
        public Musteri? YukuVeren { get; set; }
        public Musteri? YukuAlan { get; set; }
        public Musteri? TasiyiciMusteri { get; set; }

        [ForeignKey(nameof(CreatedByKullaniciID))]
        public Kullanici? CreatedByKullanici { get; set; }

        public ICollection<ForwardingFiyat> Fiyatlar { get; set; } = new List<ForwardingFiyat>();
        public ICollection<ForwardingKalem> Kalemler { get; set; } = new List<ForwardingKalem>();

        // ─── Yardımcılar ──────────────────────────────────────────────────────
        public static string DurumAdi(byte d) => d switch
        {
            0 => "Taslak",
            1 => "Aktif",
            2 => "Yüklendi",
            3 => "Teslim Edildi",
            4 => "İptal",
            _ => "—"
        };

        public static string DurumBadgeCss(byte d) => d switch
        {
            0 => "bg-secondary",
            1 => "bg-primary",
            2 => "bg-warning text-dark",
            3 => "bg-success",
            4 => "bg-danger",
            _ => "bg-light text-dark"
        };
    }
}
