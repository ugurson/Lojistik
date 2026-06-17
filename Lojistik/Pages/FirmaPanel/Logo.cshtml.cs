using Lojistik.Data;
using Lojistik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Pages.FirmaPanel;

public class LogoModel : PageModel
{
    private readonly AppDbContext        _context;
    private readonly IWebHostEnvironment _env;

    public LogoModel(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env     = env;
    }

    public Firma?  Firma   { get; set; }
    public string? Hata    { get; set; }
    public string? Basari  { get; set; }

    [BindProperty]
    public IFormFile? LogoDosya { get; set; }

    // ── Claim'den FirmaID al ──────────────────────────────────────────────
    private int? ClaimFirmaId()
    {
        var val = User.FindFirst("FirmaID")?.Value;
        return int.TryParse(val, out var id) ? id : null;
    }

    // ── GET ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync()
    {
        var firmaId = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        Firma = await _context.Firmalar.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FirmaID == firmaId.Value);

        if (Firma == null)
        {
            Hata = "Firma bilgisi bulunamadı.";
            return Page();
        }

        return Page();
    }

    // ── POST ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync()
    {
        var firmaId = ClaimFirmaId();
        if (firmaId == null) return Forbid();

        // Firma kaydını izleme modunda yükle (güncelleme için)
        Firma = await _context.Firmalar
            .FirstOrDefaultAsync(f => f.FirmaID == firmaId.Value);

        if (Firma == null)
        {
            Hata = "Firma bilgisi bulunamadı.";
            return Page();
        }

        // ── Dosya seçildi mi? ─────────────────────────────────────────────
        if (LogoDosya == null || LogoDosya.Length == 0)
        {
            Hata = "Lütfen bir logo dosyası seçin.";
            return Page();
        }

        // ── Uzantı kontrolü ───────────────────────────────────────────────
        var uzanti = Path.GetExtension(LogoDosya.FileName).ToLowerInvariant();
        string[] izinliUzantilar = { ".png", ".jpg", ".jpeg", ".webp" };
        if (!izinliUzantilar.Contains(uzanti))
        {
            Hata = "Sadece PNG, JPG, JPEG veya WEBP formatında logo yükleyebilirsiniz.";
            return Page();
        }

        // ── MIME type kontrolü ────────────────────────────────────────────
        var mime = LogoDosya.ContentType.ToLowerInvariant();
        string[] izinliMime = { "image/png", "image/jpeg", "image/webp" };
        if (!izinliMime.Contains(mime))
        {
            Hata = "Sadece PNG, JPG, JPEG veya WEBP formatında logo yükleyebilirsiniz.";
            return Page();
        }

        // ── Dosya boyutu kontrolü (500 KB) ────────────────────────────────
        if (LogoDosya.Length > 500 * 1024)
        {
            Hata = "Logo dosyası en fazla 500 KB olabilir.";
            return Page();
        }

        // ── Görsel ölçüsü kontrolü (PNG & JPEG; WEBP atlanır) ────────────
        using var stream = LogoDosya.OpenReadStream();
        var olcu = ReadImageDimensions(stream, mime);
        if (olcu.HasValue)
        {
            if (olcu.Value.width > 600 || olcu.Value.height > 300)
            {
                Hata = $"Logo boyutları çok büyük ({olcu.Value.width}×{olcu.Value.height} px). " +
                       "Maksimum: 600×300 px.";
                return Page();
            }
        }

        // ── Klasör: wwwroot/uploads/logos/{FirmaID}/ ─────────────────────
        var klasorYolu = Path.Combine(
            _env.WebRootPath, "uploads", "logos", firmaId.Value.ToString());
        Directory.CreateDirectory(klasorYolu); // yoksa oluştur

        // ── Eski logoyu sil ───────────────────────────────────────────────
        if (!string.IsNullOrEmpty(Firma.LogoDosyaAdi))
        {
            var eskiDosya = Path.Combine(klasorYolu, Firma.LogoDosyaAdi);
            if (System.IO.File.Exists(eskiDosya))
                System.IO.File.Delete(eskiDosya);
        }

        // ── Yeni dosyayı kaydet ───────────────────────────────────────────
        var dosyaAdi  = "logo" + uzanti;           // logo.png / logo.jpg / logo.webp
        var dosyaYolu = Path.Combine(klasorYolu, dosyaAdi);

        stream.Position = 0;
        await using (var fs = new FileStream(dosyaYolu, FileMode.Create, FileAccess.Write))
        {
            await stream.CopyToAsync(fs);
        }

        // ── DB güncelle ───────────────────────────────────────────────────
        Firma.LogoYolu     = $"/uploads/logos/{firmaId.Value}/{dosyaAdi}";
        Firma.LogoDosyaAdi = dosyaAdi;
        Firma.LogoMimeType = mime;

        await _context.SaveChangesAsync();

        Basari = "Logo başarıyla güncellendi.";
        return Page();
    }

    // ── Görsel ölçüsü okuma (sıfır bağımlılık) ───────────────────────────
    //
    // PNG  → Başlıktan direkt okunur (bytes 16-23).         [DESTEKLENIYOR]
    // JPEG → SOF0/SOF2 marker taranır.                      [DESTEKLENIYOR]
    // WEBP → VP8/VP8L/VP8X alt türleri karmaşık; atlanır.   [ATLANIYOR]
    //        WEBP için boyut+format kontrolü yine de geçerlidir.
    //
    private static (int width, int height)? ReadImageDimensions(Stream stream, string mime)
    {
        try
        {
            stream.Position = 0;

            if (mime == "image/png")
            {
                // PNG başlığı: [8B imza] [4B uzunluk] [4B "IHDR"] [4B genişlik] [4B yükseklik]
                var buf = new byte[24];
                if (stream.Read(buf, 0, 24) < 24) return null;
                int w = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
                int h = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
                return (w, h);
            }

            if (mime == "image/jpeg")
            {
                // JPEG: SOF0 (0xFF 0xC0) veya SOF2 (0xFF 0xC2) marker'ını tara
                int b;
                while ((b = stream.ReadByte()) != -1)
                {
                    if (b != 0xFF) continue;
                    int marker = stream.ReadByte();
                    if (marker == -1) break;

                    // SOI / EOI / padding → segment uzunluğu yok
                    if (marker == 0xD8 || marker == 0xD9 || marker == 0xFF) continue;

                    // SOF0 veya SOF2 → [2B uzunluk] [1B hassasiyet] [2B yükseklik] [2B genişlik]
                    if (marker == 0xC0 || marker == 0xC2)
                    {
                        stream.Seek(3, SeekOrigin.Current); // uzunluk (2) + hassasiyet (1)
                        var dim = new byte[4];
                        if (stream.Read(dim, 0, 4) < 4) return null;
                        int h = (dim[0] << 8) | dim[1];
                        int w = (dim[2] << 8) | dim[3];
                        return (w, h);
                    }

                    // Diğer segment → uzunluğu oku ve atla
                    var lenBuf = new byte[2];
                    if (stream.Read(lenBuf, 0, 2) < 2) return null;
                    int segLen = (lenBuf[0] << 8) | lenBuf[1];
                    if (segLen < 2) break;
                    stream.Seek(segLen - 2, SeekOrigin.Current);
                }
                return null;
            }
        }
        catch { /* parse hatası → null döner, kontrol atlanır */ }

        return null; // WEBP veya tanınmayan format
    }
}
