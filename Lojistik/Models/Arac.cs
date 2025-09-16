namespace Lojistik.Models
{
    public class Arac
    {
        public int AracID { get; set; }
        public string Plaka { get; set; } = default!;
        public string? Marka { get; set; }
        public string? Model { get; set; }
        public int? ModelYili { get; set; }
        public string? AracTipi { get; set; }
        public bool IsDorse { get; set; }
        public string? Durum { get; set; }
        public string? Notlar { get; set; }

        // 🔑 Firma ile ilişki
    public int FirmaID { get; set; }               // FK
        public Firma? Firma { get; set; }

        // (opsiyonel) hangi kullanıcı ekledi
        public int? CreatedByKullaniciID { get; set; }
        public Kullanici? CreatedByKullanici { get; set; }
        public ICollection<AracBelgesi> Belgeler { get; set; } = new List<AracBelgesi>();
        public ICollection<AracKademe> Kademeler { get; set; } = new List<AracKademe>(); // [YENİ]

    }
}
