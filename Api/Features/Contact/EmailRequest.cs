namespace CloudZen.Api.Features.Contact;

/// <summary>
/// Request model for the SendEmail function.
/// </summary>
public class EmailRequest
{
    /// <summary>
    /// The subject line of the email.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The message body of the email.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The name of the person sending the email.
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the sender.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;
}
