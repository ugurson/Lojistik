namespace Lojistik.Models;

public class Ulke
{
    public int UlkeID { get; set; }
    public string UlkeKodu { get; set; } = null!;
    public string UlkeAdi { get; set; } = null!;
    public string? UlkeAdiEN { get; set; }
    public string? TelefonKodu { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Sehir>? Sehirler { get; set; }
}
