using System;

namespace Lojistik.Models
{
    public class CariHareket
    {
        public int CariHareketID { get; set; }
        public int FirmaID { get; set; }
        public string? SubeKodu { get; set; }
        public int? KullaniciID { get; set; }

        public int MusteriID { get; set; }
        public int? IlgiliSiparisID { get; set; }
        public int? IlgiliSevkiyatID { get; set; }

        public DateTime Tarih { get; set; }
        public DateTime? VadeTarihi { get; set; }

        public string IslemTuru { get; set; } = null!;
        public string? EvrakNo { get; set; }
        public string? Aciklama { get; set; }

        public string ParaBirimi { get; set; } = "TL";
        public byte Yonu { get; set; }         // 1=Alacak, 0=Borç
        public decimal Tutar { get; set; }
        public decimal? Kur { get; set; }

        // Computed (DB’de var ama property’siz de olabilir; okumak istersen aç)
        // public decimal TutarTL { get; private set; }

        public int? CreatedByKullaniciID { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
