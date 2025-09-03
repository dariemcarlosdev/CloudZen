namespace CloudZen.Services.Abstractions
{
    public interface IEmailProvider
    {
        Task SendEmailAsync(string subject, string message, string fromName, string fromEmail);
    }
}
