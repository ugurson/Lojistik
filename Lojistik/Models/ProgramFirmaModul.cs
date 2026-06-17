using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lojistik.Models;

[Table("ProgramFirmaModulleri")]
public class ProgramFirmaModul
{
    [Key]
    public int FirmaModulID { get; set; }

    public int  FirmaID  { get; set; }
    public int  ModulID  { get; set; }
    public bool IsActive { get; set; } = true;

    public DateOnly? BaslamaTarihi { get; set; }
    public DateOnly? BitisTarihi  { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public ProgramModul?  Modul { get; set; }
    public ProgramFirma?  Firma { get; set; }
}
