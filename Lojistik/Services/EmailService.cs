using System.Net;
using System.Net.Mail;

namespace Lojistik.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException("SMTP ayarları yapılandırılmamış. appsettings.Development.json dosyasını kontrol ediniz.");
                }

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = true;
                    client.Timeout = 10000;
                    client.Credentials = new NetworkCredential(username, password);

                    var mailMessage = new MailMessage(fromEmail, to)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent to {to} with subject: {subject}");
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError($"SMTP Error sending email to {to}: {ex.Message} (StatusCode: {ex.StatusCode})");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {to}: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }
    }
}
