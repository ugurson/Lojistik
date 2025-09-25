// Models/SeferSevkiyat.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("SeferSevkiyatlar")]
    public class SeferSevkiyat
    {
        [Key] public int SeferSevkiyatID { get; set; }
        [Required] public int FirmaID { get; set; }
        [StringLength(20)] public string? SubeKodu { get; set; }
        public int? KullaniciID { get; set; }

        [Required] public int SeferID { get; set; }
        [Required] public int SevkiyatID { get; set; }

        [Required] public byte Yon { get; set; } // 0=gidiş,1=dönüş (örnek)
        [StringLength(300)] public string? Notlar { get; set; }

        [Required] public DateTime CreatedAt { get; set; }

        // Navigations
        public Firma? Firma { get; set; }
        public Kullanici? Kullanici { get; set; }

        public Sefer? Sefer { get; set; }
        public Sevkiyat? Sevkiyat { get; set; }
    }
}
