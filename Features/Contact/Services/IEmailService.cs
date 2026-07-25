namespace CloudZen.Features.Contact.Services;

/// <summary>
/// Interface for email service that sends emails via API backend.
/// This is the recommended approach for Blazor WebAssembly applications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email through the backend API.
    /// </summary>
    /// <param name="subject">Email subject</param>
    /// <param name="message">Email body message</param>
    /// <param name="fromName">Sender's name</param>
    /// <param name="fromEmail">Sender's email address</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<EmailResult> SendEmailAsync(string subject, string message, string fromName, string fromEmail);
}

/// <summary>
/// Result of an email send operation.
/// </summary>
public class EmailResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }

    public static EmailResult Ok(string? message = null) => new() { Success = true, Message = message };
    public static EmailResult Fail(string error) => new() { Success = false, Error = error };
}
