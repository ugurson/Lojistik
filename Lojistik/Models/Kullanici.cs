namespace Lojistik.Models;

public class Kullanici
{
    public int KullaniciID { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;      // ilk etapta düz metin, sonra hash
    public string? KullaniciAdi { get; set; }
    public int FirmaID { get; set; }
    public string? SubeKodu { get; set; }
    public string? AltSubeKodu { get; set; }
    public int? YetkiSeviyesi1 { get; set; }
    public int? YetkiSeviyesi2 { get; set; }

    public Firma? Firma { get; set; }
}
