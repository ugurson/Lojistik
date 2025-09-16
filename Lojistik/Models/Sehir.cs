namespace Lojistik.Models;

public class Sehir
{
    public int SehirID { get; set; }
    public int UlkeID { get; set; }
    public string SehirAdi { get; set; } = null!;
    public string? SehirKodu { get; set; }
    public bool IsActive { get; set; } = true;

    public Ulke? Ulke { get; set; }
}
