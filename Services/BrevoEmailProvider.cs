using CloudZen.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudZen.Services
{
    public class BrevoEmailProvider : IEmailProvider
    {
        private readonly IConfiguration _config;

        public BrevoEmailProvider(IConfiguration config)
        {
            _config = config;
        }

        public async System.Threading.Tasks.Task SendEmailAsync(string subject, string message, string fromName, string fromEmail)
        {
            // Configure Brevo client
            Configuration.Default.ApiKey["api-key"] = _config["EmailSettings:BrevoApiKey"];
            var apiInstance = new TransactionalEmailsApi();

            // SendSmtpEmailSender is the sender object for the email (From)
            var sender = new SendSmtpEmailSender() { Email= _config["EmailSettings:FromEmail"] };
            //var recipient = new SendSmtpEmailTo(_config["EmailSettings:ToEmail"]);
            var recipient = new SendSmtpEmailTo(_config["EmailSettings:FromEmail"], fromName);
            var cc = new SendSmtpEmailCc(_config["EmailSettings:CcEmail"]);

            var email = new SendSmtpEmail(
                sender: sender,
                to: new List<SendSmtpEmailTo> { recipient },
                cc: new List<SendSmtpEmailCc> { cc },
                subject: subject,
                htmlContent: $"<strong>From:</strong> {fromName} ({fromEmail})<br/><br/>{message}",
                textContent: $"From: {fromName} ({fromEmail})\n\n{message}"
            );

            await apiInstance.SendTransacEmailAsync(email);
        }
    }
}
