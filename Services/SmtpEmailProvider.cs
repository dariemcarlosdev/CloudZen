using CloudZen.Services.Abstractions;
using System.Net;
using System.Net.Mail;

namespace CloudZen.Services
{
    public class SmtpEmailProvider : IEmailProvider
    {
        private readonly IConfiguration _config;
        public SmtpEmailProvider(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string subject, string message, string fromName, string fromEmail)
        {
            var client = new SmtpClient(_config["EmailSettings:SmtpHost"], int.Parse(_config["EmailSettings:SmtpPort"]))
            {
                Credentials = new NetworkCredential(
                    _config["EmailSettings:SmtpUser"],
                    _config["EmailSettings:SmtpPass"]),
                EnableSsl = true
            };

            var emailMessage = new MailMessage(fromEmail, _config["EmailSettings:ToEmail"], subject, message);
            await client.SendMailAsync(emailMessage);
        }
    }
}
