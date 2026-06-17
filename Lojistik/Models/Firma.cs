namespace Lojistik.Models;

public class Firma
{
    public int FirmaID { get; set; }
    public string FirmaKodu { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string FirmaAdi { get; set; } = null!;
    public string? LogoYolu { get; set; }
    public string? LogoDosyaAdi { get; set; }
    public string? LogoMimeType { get; set; }
    public ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
}