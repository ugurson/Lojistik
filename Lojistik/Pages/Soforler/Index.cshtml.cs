using System.Linq;
using System.Threading.Tasks;
using Lojistik.Data;
using Lojistik.Extensions; // User.GetFirmaId()
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.Soforler
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public class Row
        {
            public int SoforID { get; set; }
            public string AdSoyad { get; set; } = "";
            public string? Telefon { get; set; }
            public string? EhliyetSinifi { get; set; }
            public string? SurucuKartNo { get; set; }
            public byte Durum { get; set; }
            public DateTime? EhliyetGecerlilikTarihi { get; set; }
            public DateTime? PasaportBitisTarihi { get; set; }
            public DateTime? VizeBitisTarihi { get; set; }
        }

        public IList<Row> Items { get; set; } = new List<Row>();

        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public string? durum { get; set; } // "Aktif" / "Pasif" / null
        [BindProperty(SupportsGet = true)] public string? sort { get; set; } = "ad"; // ad|durum

        public async Task OnGetAsync()
        {
            var firmaId = User.GetFirmaId();

            var query = _context.Soforler.AsNoTracking()
                .Where(s => s.FirmaID == firmaId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(s =>
                    s.AdSoyad.Contains(t) ||
                    (s.TCKimlikNo != null && s.TCKimlikNo.Contains(t)) ||
                    (s.Telefon != null && s.Telefon.Contains(t)) ||
                    (s.SurucuKartNo != null && s.SurucuKartNo.Contains(t)) ||
                    (s.EhliyetNo != null && s.EhliyetNo.Contains(t)) ||
                    (s.PasaportNo != null && s.PasaportNo.Contains(t)) ||
                    (s.Eposta != null && s.Eposta.Contains(t))
                );
            }

            if (durum == "Aktif") query = query.Where(s => s.Durum == 1);
            else if (durum == "Pasif") query = query.Where(s => s.Durum == 0);

            query = sort switch
            {
                "durum" => query.OrderByDescending(s => s.Durum).ThenBy(s => s.AdSoyad),
                _ => query.OrderBy(s => s.AdSoyad),
            };

            Items = await query.Select(s => new Row
            {
                SoforID = s.SoforID,
                AdSoyad = s.AdSoyad,
                Telefon = s.Telefon,
                EhliyetSinifi = s.EhliyetSinifi,
                SurucuKartNo = s.SurucuKartNo,
                Durum = s.Durum,
                EhliyetGecerlilikTarihi = s.EhliyetGecerlilikTarihi,
                PasaportBitisTarihi = s.PasaportBitisTarihi,
                VizeBitisTarihi = s.VizeBitisTarihi
            }).ToListAsync();
        }
    }
}
