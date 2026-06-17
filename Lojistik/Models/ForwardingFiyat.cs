using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("ForwardingFiyatlar")]
    public class ForwardingFiyat
    {
        [Key] public int FiyatID { get; set; }
        [Required] public int FirmaID { get; set; }
        [Required] public int ForwardingID { get; set; }

        // 'Satis' veya 'Alis'
        [Required, StringLength(10)] public string FiyatTuru { get; set; } = "Satis";

        // Navlun, Gümrük, Sigorta, Diğer...
        [StringLength(50)] public string? Kalem { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")] public decimal Tutar { get; set; }
        [Required, StringLength(10)] public string ParaBirimi { get; set; } = "EUR";

        [StringLength(200)] public string? Aciklama { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ForwardingIs? ForwardingIs { get; set; }
    }
}
