using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Shikayat.Application.Interfaces;
using System.Net.Mail;
using System.Net;

namespace Shikayat.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _config;

        public EmailService(ILogger<EmailService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // 1. Filter Test Users
            if (string.IsNullOrEmpty(toEmail) || toEmail.EndsWith("@shikayat.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"[Email Skipped] To: {toEmail} | Subject: {subject} | Reason: Test User Domain");
                return;
            }

            try
            {
                var host = _config["EmailSettings:Host"];
                var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var password = _config["EmailSettings:Password"];
                var displayName = _config["EmailSettings:DisplayName"];

                if (string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Email password is not configured. Email will not be sent.");
                    return;
                }

                using (var client = new SmtpClient(host, port))
                {
                    client.Credentials = new NetworkCredential(senderEmail, password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, displayName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false // Simple text for now
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"[Email Sent] To: {toEmail} | Subject: {subject}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Email Failed] To: {toEmail}");
                throw; // Rethrow to let the caller (or test endpoint) see the error
            }
        }
    }
}
