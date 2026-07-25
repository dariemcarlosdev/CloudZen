namespace CloudZen.Features.Contact;

/// <summary>
/// Configuration options for the email service client.
/// </summary>
/// <remarks>
/// <para>
/// This class is used with the ASP.NET Core Options pattern to configure the 
/// <see cref="Services.ApiEmailService"/>. Settings are configured in <c>appsettings.json</c>
/// under the <c>EmailService</c> section.
/// </para>
/// <para>
/// <b>Important:</b> In Blazor WebAssembly, do NOT store sensitive values like API keys here.
/// API keys should only exist in the Azure Functions backend configuration.
/// </para>
/// <para>
/// Example configuration in <c>wwwroot/appsettings.json</c>:
/// <code>
/// {
///   "EmailService": {
///     "ApiBaseUrl": "/api",
///     "TimeoutSeconds": 30,
///     "MaxRetries": 3
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public class EmailServiceOptions
{
    /// <summary>
    /// The configuration section name for email service options.
    /// </summary>
    public const string SectionName = "EmailService";

    /// <summary>
    /// Gets or sets the base URL for the email API backend.
    /// </summary>
    /// <value>
    /// The API base URL. Defaults to "/api" for Azure Static Web Apps linked functions.
    /// </value>
    /// <remarks>
    /// For local development, this might be "http://localhost:7071/api".
    /// For production with linked Azure Functions, use "/api".
    /// </remarks>
    public string ApiBaseUrl { get; set; } = "/api";

    /// <summary>
    /// Gets or sets the HTTP request timeout in seconds.
    /// </summary>
    /// <value>The timeout in seconds. Defaults to 30.</value>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed requests.
    /// </summary>
    /// <value>The maximum retries. Defaults to 3.</value>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the email endpoint path (appended to ApiBaseUrl).
    /// </summary>
    /// <value>The endpoint path. Defaults to "send-email".</value>
    public string SendEmailEndpoint { get; set; } = "send-email";

    /// <summary>
    /// Gets the full URL for the send email endpoint.
    /// </summary>
    public string SendEmailUrl => $"{ApiBaseUrl.TrimEnd('/')}/{SendEmailEndpoint}";
}
