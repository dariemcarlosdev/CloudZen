namespace CloudZen.Models.Options;

/// <summary>
/// Configuration options for the chatbot service client.
/// </summary>
/// <remarks>
/// <para>
/// This class is used with the ASP.NET Core Options pattern to configure the
/// <see cref="Services.ChatbotService"/>. Settings are configured in <c>appsettings.json</c>
/// under the <c>ChatbotService</c> section.
/// </para>
/// <para>
/// <b>Important:</b> In Blazor WebAssembly, do NOT store API keys here.
/// The Anthropic API key is stored only in the Azure Functions backend configuration.
/// </para>
/// <para>
/// Example configuration in <c>wwwroot/appsettings.json</c>:
/// <code>
/// {
///   "ChatbotService": {
///     "ApiBaseUrl": "/api",
///     "TimeoutSeconds": 30,
///     "ChatEndpoint": "chat"
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public class ChatbotOptions
{
    /// <summary>
    /// The configuration section name for chatbot service options.
    /// </summary>
    public const string SectionName = "ChatbotService";

    /// <summary>
    /// Gets or sets the base URL for the chatbot API backend.
    /// </summary>
    /// <value>
    /// The API base URL. Defaults to "/api" for Azure Static Web Apps linked functions.
    /// </value>
    public string ApiBaseUrl { get; set; } = "/api";

    /// <summary>
    /// Gets or sets the HTTP request timeout in seconds.
    /// </summary>
    /// <value>The timeout in seconds. Defaults to 60 (longer than email due to AI processing).</value>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the chat endpoint path (appended to ApiBaseUrl).
    /// </summary>
    /// <value>The endpoint path. Defaults to "chat".</value>
    public string ChatEndpoint { get; set; } = "chat";

    /// <summary>
    /// Gets the full URL for the chat endpoint.
    /// </summary>
    public string ChatUrl => $"{ApiBaseUrl.TrimEnd('/')}/{ChatEndpoint}";
}
