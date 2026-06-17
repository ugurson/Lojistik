using Lojistik.Extensions;
using Lojistik.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lojistik.Pages.DilekIstek
{
    public class IndexModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(IEmailService emailService, ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public string Mesaj { get; set; } = "";

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Mesaj))
            {
                ErrorMessage = "Lütfen bir mesaj giriniz.";
                return Page();
            }

            try
            {
                var kullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
                var subject = "Lojistik Sistemi - Dilek & İstek";
                var body = $@"
                    <h3>Yeni Dilek & İstek Mesajı</h3>
                    <p><strong>Kullanıcı:</strong> {kullaniciAdi}</p>
                    <p><strong>Tarih:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
                    <hr/>
                    <p><strong>Mesaj:</strong></p>
                    <p>{System.Text.RegularExpressions.Regex.Replace(Mesaj, @"\r?\n", "<br/>")}</p>
                ";

                var adminEmail = _configuration["AdminEmail"] ?? "ugur@akinal.net";
                await _emailService.SendEmailAsync(adminEmail, subject, body);

                SuccessMessage = "Mesajınız başarıyla gönderilmiştir. Teşekkür ederiz!";
                Mesaj = "";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Configuration error: {ex.Message}");
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending feedback email: {ex.GetType().Name} - {ex.Message}");
                ErrorMessage = $"Mesaj gönderilirken bir hata oluştu. Sistem Yöneticisine danışınız. ({ex.GetType().Name})";
            }

            return Page();
        }
    }
}
