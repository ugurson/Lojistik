namespace Lojistik.Models;

public class Firma
{
    public int FirmaID { get; set; }
    public string FirmaKodu { get; set; } = null!;

    public ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
}