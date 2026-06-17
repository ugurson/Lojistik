using System;
using System.ComponentModel.DataAnnotations;

namespace Lojistik.Models
{
    public class KademeFirma
    {
        public int KademeFirmaID { get; set; }

        public int FirmaID { get; set; }

        [Required, StringLength(120)]
        public string Unvan { get; set; } = "";

        [StringLength(20)]
        public string? VergiNo { get; set; }

        [StringLength(30)]
        public string? Telefon { get; set; }

        public bool Aktif { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
