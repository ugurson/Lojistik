using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models
{
    [Table("ProgramAltSubeler")]
    public class ProgramAltSube
    {
        [Key] public int AltSubeID { get; set; }

        public int FirmaID { get; set; }

        [StringLength(20)]
        public string AltSubeKodu { get; set; } = null!;

        [StringLength(200)]
        public string AltSubeAdi { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Notlar { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
