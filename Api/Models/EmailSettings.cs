namespace CloudZen.Api.Models;

/// <summary>
/// Configuration options for email sending functionality.
/// </summary>
/// <remarks>
/// <para>
/// This class is used with the ASP.NET Core Options pattern to configure email settings
/// for the <see cref="Functions.SendEmailFunction"/>. Settings can be configured in 
/// environment variables or <c>local.settings.json</c>.
/// </para>
/// <para>
/// Example configuration in <c>local.settings.json</c>:
/// <code>
/// {
///   "Values": {
///     "EmailSettings:FromEmail": "noreply@example.com",
///     "EmailSettings:CcEmail": "admin@example.com",
///     "EmailSettings:ToEmail": "contact@example.com"
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public class EmailSettings
{
    /// <summary>
    /// The configuration section name for email settings.
    /// </summary>
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    /// <value>The email address used as the sender. Defaults to "cloudzen.inc@gmail.com".</value>
    public string FromEmail { get; set; } = "cloudzen.inc@gmail.com";

    /// <summary>
    /// Gets or sets the CC email address for notifications.
    /// </summary>
    /// <value>The CC email address, or <c>null</c> if no CC is needed.</value>
    public string? CcEmail { get; set; }

    /// <summary>
    /// Gets or sets the recipient email address for contact form submissions.
    /// </summary>
    /// <value>The recipient email address. Defaults to the same as <see cref="FromEmail"/>.</value>
    public string? ToEmail { get; set; }

    /// <summary>
    /// Gets or sets the display name for the sender.
    /// </summary>
    /// <value>The sender display name. Defaults to "CloudZen Contact".</value>
    public string FromName { get; set; } = "CloudZen Contact";
}
