using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("AraclarKademe")] // [YENİ] DB'deki tablo adı
    public class AracKademe
    {
        [Key]
        public int KademeID { get; set; }

        [Required]
        public int AracID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Tarih")]
        public DateTime Tarih { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Yapılan İşlem")]
        public string YapilanIslem { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Tutar")]
        public decimal Tutar { get; set; }

        [Required, StringLength(10)]
        [Display(Name = "Para Birimi")]
        public string ParaBirimi { get; set; } = "TRY";

        [Display(Name = "Notlar")]
        public string? Notlar { get; set; }

        [Display(Name = "Oluşturma")]
        public DateTime CreatedAt { get; set; } // DB default GETDATE() alacak
                                                // (elle set etmeye gerek yok)

        // Navigasyon
        public Arac? Arac { get; set; } // [YENİ]
    }
}
