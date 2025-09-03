using CloudZen.Services.Abstractions;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CloudZen.Services
{
    public class SendGridEmailProvider : IEmailProvider
    {
        private readonly IConfiguration _config;

        public SendGridEmailProvider(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string subject, string message, string fromName, string fromEmail)
        {
            var client = new SendGridClient(_config["EmailSettings:SendGridApiKey"]);
            var from = new EmailAddress(_config["EmailSettings:FromEmail"], "CloudZen Website");
            var to = new EmailAddress(_config["EmailSettings:ToEmail"]);

            var msg = MailHelper.CreateSingleEmail(
            from,
            to,
            subject,
            $"From: {fromName} ({fromEmail})\n\n{message}",
            $"<strong>From:</strong> {fromName} ({fromEmail})<br/><br/>{message}"
            );

            await client.SendEmailAsync(msg);
        }
    }
}
