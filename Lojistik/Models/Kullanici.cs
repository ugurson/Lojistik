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
    public byte AracYetkisi { get; set; } = 0;

    public byte SiparisYetkisi { get; set; } = 0;
    public byte SeferYetkisi { get; set; } = 0;
    public byte MusteriYetkisi { get; set; } = 0;
    public byte CariYetkisi { get; set; } = 0;
    public byte RaporYetkisi { get; set; } = 0;
    public byte ForwardingYetkisi { get; set; } = 0;

    public bool IsActive      { get; set; } = true;
    public bool IsFirmaAdmin  { get; set; } = false;
    public bool IsSistemAdmin { get; set; } = false;

    public DateTime  CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Firma? Firma { get; set; }
}
