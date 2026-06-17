using System.ComponentModel.DataAnnotations;

namespace Lojistik.Models;

public class GuncellemeNotu
{
    public int GuncellemeNotuID { get; set; }

    [Required, StringLength(200)]
    public string Baslik { get; set; } = null!;

    [StringLength(4000)]
    public string? Aciklama { get; set; }

    [StringLength(100)]
    public string? Bolum { get; set; }

    [StringLength(20)]
    public string? Surum { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
