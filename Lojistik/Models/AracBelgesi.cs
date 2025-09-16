namespace Lojistik.Models
{
    public class AracBelgesi
    {
        public int BelgeID { get; set; }
        public int AracID { get; set; }
        public string BelgeTipi { get; set; } = default!;
        public string? BelgeNo { get; set; }
        public string? Firma { get; set; }
        public DateOnly BaslangicTarihi { get; set; }
        public DateOnly? BitisTarihi { get; set; }   // NULL = aktif
        public decimal? Tutar { get; set; }
        public string? ParaBirimi { get; set; }
        public string? DosyaYolu { get; set; }
        public string? Notlar { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Arac? Arac { get; set; }
    }
}
