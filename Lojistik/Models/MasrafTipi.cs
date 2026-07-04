using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models;

// Sefer masrafı eklerken kullanılan masraf tipleri (global lookup — tüm firmalar aynı liste)
[Table("MasrafTipleri")]
public class MasrafTipi
{
    [Key] public int MasrafTipID { get; set; }

    [Required, StringLength(50)] public string Ad { get; set; } = null!;

    public int SiraNo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
