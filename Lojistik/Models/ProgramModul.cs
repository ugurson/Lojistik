using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models;

[Table("ProgramModulleri")]
public class ProgramModul
{
    [Key]
    public int ModulID { get; set; }

    [Required, StringLength(50)]
    public string ModulKodu { get; set; } = null!;

    [Required, StringLength(100)]
    public string ModulAdi { get; set; } = null!;

    [StringLength(300)]
    public string? Aciklama { get; set; }

    public bool IsActive { get; set; } = true;
    public int  SiraNo   { get; set; } = 0;
}
