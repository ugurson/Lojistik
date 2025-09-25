// Models/SeferMasraf.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("SeferMasraflari")]
    public class SeferMasraf
    {
        [Key] public int SeferMasrafID { get; set; }
        [Required] public int FirmaID { get; set; }
        [StringLength(20)] public string? SubeKodu { get; set; }
        public int? KullaniciID { get; set; }

        [Required] public int SeferID { get; set; }
        [Required] public DateTime Tarih { get; set; } // date

        [Required, StringLength(50)] public string MasrafTipi { get; set; } = null!;
        [Required] public decimal Tutar { get; set; }
        [Required, StringLength(10)] public string ParaBirimi { get; set; } = null!;

        [StringLength(50)] public string? FaturaBelgeNo { get; set; }
        [StringLength(50)] public string? Ulke { get; set; }
        [StringLength(100)] public string? Yer { get; set; }
        [StringLength(300)] public string? Notlar { get; set; }

        public int? CreatedByKullaniciID { get; set; }
        [Required] public DateTime CreatedAt { get; set; }

        // Navigations
        public Firma? Firma { get; set; }
        public Kullanici? Kullanici { get; set; }
        [ForeignKey(nameof(CreatedByKullaniciID))] public Kullanici? CreatedByKullanici { get; set; }
        public Sefer? Sefer { get; set; }
    }
}
