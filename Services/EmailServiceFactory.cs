using CloudZen.Services.Abstractions;

namespace CloudZen.Services
{
    // Factory class to create email provider instances based on configuration
    // Factory Pattern implementation for email providers
    // This Design Pattern allows for easy extension to support new email providers in the future and decouples the email sending logic from the specific provider implementations.
    public class EmailServiceFactory : IEmailProvider
    {
        // Inject configuration to determine which provider to use
        private readonly IEmailProvider _provider;
        public EmailServiceFactory(IConfiguration config)
        {
           var providerName = config["EmailSettings:Provider"];

            // Instantiate the appropriate provider based on configuration
            _provider = providerName switch
              {
                "SendGrid" => new SendGridEmailProvider(config),
                // Add other providers here as needed
                "SMTP" => new SmtpEmailProvider(config),
                  "Brevo" => new BrevoEmailProvider(config),
                  _ => throw new NotImplementedException($"Email provider '{providerName}' is not implemented.")
              };
        }

        public async Task SendEmailAsync(string subject, string message, string fromName, string fromEmail)
        {
           await _provider.SendEmailAsync(subject, message, fromName, fromEmail);
        }
    }
}
