namespace CloudZen.Models;

/// <summary>
/// Request model for sending emails through the API backend.
/// </summary>
/// <remarks>
/// This model is serialized to JSON and sent to the <c>SendEmail</c> Azure Function endpoint.
/// The property names match the expected JSON structure of the API.
/// </remarks>
public class EmailApiRequest
{
    /// <summary>
    /// Gets or sets the email subject line.
    /// </summary>
    /// <value>The subject text. Maximum 200 characters enforced by the API.</value>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email body message.
    /// </summary>
    /// <value>The message content. Maximum 5000 characters enforced by the API.</value>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender's display name.
    /// </summary>
    /// <value>The name to display as the sender. Maximum 100 characters enforced by the API.</value>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    /// <value>A valid email address format is required.</value>
    public string FromEmail { get; set; } = string.Empty;
}
