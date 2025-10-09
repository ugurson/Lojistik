// Models/Sofor.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Lojistik.Models
{
    public class Sofor
    {
        public int SoforID { get; set; }

        [Required]
        public int FirmaID { get; set; }
        public string? SubeKodu { get; set; }
        public int? CreatedByKullaniciID { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required, StringLength(120)]
        public string AdSoyad { get; set; } = string.Empty;

        [StringLength(11)]
        public string? TCKimlikNo { get; set; }

        [StringLength(40)]
        public string? Uyruk { get; set; }

        [StringLength(30)]
        public string? Telefon { get; set; }

        [EmailAddress, StringLength(120)]
        public string? Eposta { get; set; }

        [StringLength(40)]
        public string? EhliyetNo { get; set; }

        [StringLength(20)]
        public string? EhliyetSinifi { get; set; }

        public DateTime? EhliyetVerilisTarihi { get; set; }
        public DateTime? EhliyetGecerlilikTarihi { get; set; }

        [StringLength(40)]
        public string? SRCBelgeNo { get; set; }

        [StringLength(40)]
        public string? PsikoteknikNo { get; set; }

        [StringLength(40)]
        public string? PasaportNo { get; set; }
        public DateTime? PasaportBitisTarihi { get; set; }
        public DateTime? VizeBitisTarihi { get; set; }

        [StringLength(40)]
        public string? SurucuKartNo { get; set; }

        public DateTime? DogumTarihi { get; set; }

        [StringLength(10)]
        public string? KanGrubu { get; set; }

        [StringLength(40)]
        public string? SGKNo { get; set; }

        [StringLength(34)]
        public string? IBAN { get; set; }

        /// <summary>1: Aktif, 0: Pasif</summary>
        [Range(0, 1)]
        public byte Durum { get; set; } = 1;

        public string? Notlar { get; set; }

        // (Opsiyonel) Navigation’lar eklenebilir:
        // public Firma? Firma { get; set; }
        // public Kullanici? CreatedBy { get; set; }
    }
}
