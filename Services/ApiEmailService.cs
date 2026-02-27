using CloudZen.Models;
using CloudZen.Models.Options;
using CloudZen.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudZen.Services;

/// <summary>
/// Email service implementation that sends emails through the Azure Functions API backend.
/// </summary>
/// <remarks>
/// <para>
/// This is the recommended and secure approach for Blazor WebAssembly applications,
/// ensuring that sensitive API keys remain on the server side and are never exposed to the client browser.
/// </para>
/// <para>
/// The service communicates with the <c>SendEmail</c> Azure Function endpoint, which handles:
/// <list type="bullet">
///   <item><description>Rate limiting to prevent abuse</description></item>
///   <item><description>Input validation and sanitization</description></item>
///   <item><description>Secure API key management</description></item>
///   <item><description>Email delivery via Brevo (formerly Sendinblue)</description></item>
/// </list>
/// </para>
/// <para>
/// Configuration (in <c>appsettings.json</c>):
/// <code>
/// {
///   "EmailService": {
///     "ApiBaseUrl": "/api",
///     "TimeoutSeconds": 30,
///     "MaxRetries": 3,
///     "SendEmailEndpoint": "send-email"
///   }
/// }
/// </code>
/// </para>
/// </remarks>
/// <example>
/// Register the service in <c>Program.cs</c>:
/// <code>
/// builder.Services.Configure&lt;EmailServiceOptions&gt;(
///     builder.Configuration.GetSection(EmailServiceOptions.SectionName));
/// builder.Services.AddScoped&lt;IEmailService, ApiEmailService&gt;();
/// </code>
/// </example>
public class ApiEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly EmailServiceOptions _options;
    private readonly ILogger<ApiEmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEmailService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to communicate with the API backend.</param>
    /// <param name="options">The email service configuration options.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <remarks>
    /// Uses the IOptions pattern for strongly-typed configuration access.
    /// Configuration is read from the "EmailService" section of appsettings.json.
    /// </remarks>
    public ApiEmailService(
        HttpClient httpClient, 
        IOptions<EmailServiceOptions> options, 
        ILogger<ApiEmailService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Configure HTTP client timeout from options
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <summary>
    /// Sends an email through the Azure Functions API backend.
    /// </summary>
    /// <param name="subject">The subject line of the email. Maximum 200 characters.</param>
    /// <param name="message">The body content of the email. Maximum 5000 characters.</param>
    /// <param name="fromName">The display name of the sender. Maximum 100 characters.</param>
    /// <param name="fromEmail">The email address of the sender. Must be a valid email format.</param>
    /// <returns>
    /// An <see cref="EmailResult"/> indicating success or failure:
    /// <list type="bullet">
    ///   <item><description><see cref="EmailResult.Success"/> = <c>true</c> with confirmation message on success</description></item>
    ///   <item><description><see cref="EmailResult.Success"/> = <c>false</c> with error details on failure</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method handles the following error scenarios gracefully:
    /// <list type="bullet">
    ///   <item><description><b>Network errors</b> - Returns user-friendly connection error message</description></item>
    ///   <item><description><b>Timeout</b> - Returns timeout message suggesting retry</description></item>
    ///   <item><description><b>API errors (4xx/5xx)</b> - Parses and returns the API error message</description></item>
    ///   <item><description><b>Rate limiting (429)</b> - Returns the rate limit error from the API</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The API endpoint performs server-side validation. Invalid inputs will result in a 
    /// <see cref="EmailResult"/> with <see cref="EmailResult.Success"/> = <c>false</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await emailService.SendEmailAsync(
    ///     subject: "Contact Request",
    ///     message: "I would like to discuss a project.",
    ///     fromName: "John Doe",
    ///     fromEmail: "john@example.com"
    /// );
    /// 
    /// if (result.Success)
    /// {
    ///     Console.WriteLine(result.Message);
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Error: {result.Error}");
    /// }
    /// </code>
    /// </example>
    public async Task<EmailResult> SendEmailAsync(string subject, string message, string fromName, string fromEmail)
    {
        try
        {
            var request = new EmailApiRequest
            {
                Subject = subject,
                Message = message,
                FromName = fromName,
                FromEmail = fromEmail
            };

            var endpoint = _options.SendEmailUrl;
            _logger.LogInformation("Sending email request to {Endpoint}", endpoint);

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EmailApiResponse>();
                _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", result?.MessageId);
                return EmailResult.Ok(result?.Message ?? "Email sent successfully.");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Email API returned error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);

                try
                {
                    var errorResult = JsonSerializer.Deserialize<EmailApiErrorResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return EmailResult.Fail(errorResult?.Error ?? "Failed to send email.");
                }
                catch
                {
                    return EmailResult.Fail($"Failed to send email. Status: {response.StatusCode}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending email: {Message}", ex.Message);
            return EmailResult.Fail("Unable to connect to email service. Please check your internet connection.");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.CancellationToken.IsCancellationRequested == false)
        {
            _logger.LogError(ex, "Timeout sending email after {TimeoutSeconds} seconds", _options.TimeoutSeconds);
            return EmailResult.Fail("Request timed out. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email: {Message}", ex.Message);
            return EmailResult.Fail("An unexpected error occurred. Please try again later.");
        }
    }
}
