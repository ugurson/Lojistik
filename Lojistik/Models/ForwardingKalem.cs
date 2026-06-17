using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("ForwardingKalemler")]
    public class ForwardingKalem
    {
        [Key] public int KalemID { get; set; }
        [Required] public int FirmaID { get; set; }
        [Required] public int ForwardingID { get; set; }

        /// <summary>Miktar — örn: 2, 0.5</summary>
        [Column(TypeName = "decimal(10,3)")]
        public decimal Adet { get; set; }

        /// <summary>Birim — Palet, Koli, Kutu, Çuval, Big Bag, Varil, Konteyner, Adet, Ton, Diğer</summary>
        [Required, StringLength(50)]
        public string Nevi { get; set; } = "Palet";

        [StringLength(200)]
        public string? Aciklama { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ForwardingIs? ForwardingIs { get; set; }
    }
}
