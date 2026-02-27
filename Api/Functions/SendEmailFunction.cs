using CloudZen.Api.Models;
using CloudZen.Api.Security;
using CloudZen.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System.Text.Json;

namespace CloudZen.Api.Functions;

/// <summary>
/// Azure Function to handle email sending through the Brevo (formerly Sendinblue) API.
/// </summary>
/// <remarks>
/// This function serves as a secure backend for the Blazor WebAssembly frontend contact form,
/// ensuring that API keys remain secure on the server side and are never exposed to the client.
/// <para>
/// Security features implemented:
/// <list type="bullet">
///   <item><description>Rate limiting to prevent abuse and DDoS attacks</description></item>
///   <item><description>Input validation and sanitization to prevent XSS and injection attacks</description></item>
///   <item><description>Security headers added to all responses</description></item>
///   <item><description>Request body size limiting</description></item>
///   <item><description>Correlation ID tracking for request tracing</description></item>
/// </list>
/// </para>
/// </remarks>
public class SendEmailFunction
{
    private readonly ILogger<SendEmailFunction> _logger;
    private readonly IConfiguration _config;
    private readonly IRateLimiterService _rateLimiter;
    private readonly CorsSettings _corsSettings;
    private readonly EmailSettings _emailSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendEmailFunction"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="config">The configuration provider for accessing secrets (API keys).</param>
    /// <param name="rateLimiter">The rate limiter service for throttling requests.</param>
    /// <param name="corsSettings">The CORS settings for cross-origin requests.</param>
    /// <param name="emailSettings">The email configuration settings.</param>
    public SendEmailFunction(
        ILogger<SendEmailFunction> logger,
        IConfiguration config,
        IRateLimiterService rateLimiter,
        CorsSettings corsSettings,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _config = config;
        _rateLimiter = rateLimiter;
        _corsSettings = corsSettings;
        _emailSettings = emailSettings.Value;
    }

    /// <summary>
    /// HTTP POST endpoint to send emails via the Brevo transactional email API.
    /// Also handles OPTIONS preflight requests for CORS.
    /// </summary>
    /// <param name="req">The HTTP request containing an <see cref="EmailRequest"/> JSON body.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing:
    /// <list type="bullet">
    ///   <item><description><b>200 OK</b> - Email sent successfully with message ID</description></item>
    ///   <item><description><b>204 No Content</b> - For CORS preflight requests</description></item>
    ///   <item><description><b>400 Bad Request</b> - Invalid request body, validation failure, or malformed JSON</description></item>
    ///   <item><description><b>429 Too Many Requests</b> - Rate limit exceeded (includes Retry-After header)</description></item>
    ///   <item><description><b>500 Internal Server Error</b> - Email service configuration error or Brevo API failure</description></item>
    /// </list>
    /// </returns>
    [Function("SendEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "send-email")] HttpRequest req)
    {
        // Add CORS headers to all responses
        req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);

        // Handle CORS preflight requests
        if (req.IsCorsPreflightRequest())
        {
            return new StatusCodeResult(StatusCodes.Status204NoContent);
        }

        // Add security headers to response
        req.HttpContext.Response.AddSecurityHeaders();

        // Get client IP for rate limiting and logging
        var clientIp = req.GetClientIpAddress();
        var correlationId = req.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ClientIp"] = InputValidator.SanitizeForLogging(clientIp)
        });

        _logger.LogInformation("SendEmail function triggered from {ClientIp}", InputValidator.SanitizeForLogging(clientIp));

        try
        {
            // Check rate limit
            var rateLimitResult = await _rateLimiter.TryAcquireAsync(clientIp, "send-email");
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientIp}", InputValidator.SanitizeForLogging(clientIp));

                req.HttpContext.Response.Headers.TryAdd("Retry-After", rateLimitResult.RetryAfter?.TotalSeconds.ToString("F0") ?? "60");

                return new ObjectResult(new { error = rateLimitResult.Message })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
            }

            // Read and parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Empty request body received.");
                return new BadRequestObjectResult(new { error = "Request body is required." });
            }

            // Limit request body size
            if (requestBody.Length > 10000)
            {
                _logger.LogWarning("Request body too large: {Size} bytes", requestBody.Length);
                return new BadRequestObjectResult(new { error = "Request body too large." });
            }

            var emailRequest = JsonSerializer.Deserialize<EmailRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                MaxDepth = 10 // Prevent deep object attacks
            });

            if (emailRequest == null)
            {
                _logger.LogWarning("Failed to deserialize email request.");
                return new BadRequestObjectResult(new { error = "Invalid request format." });
            }

            // Validate required fields with security checks
            var validationError = ValidateEmailRequest(emailRequest);
            if (validationError != null)
            {
                _logger.LogWarning("Validation failed: {Error}", validationError);
                return new BadRequestObjectResult(new { error = validationError });
            }

            // Get Brevo API key from configuration (environment variable or Key Vault)
            // Note: API keys should NOT be in IOptions - they come from secure configuration
            var apiKey = _config["BREVO_API_KEY"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Brevo API key is not configured.");
                return new ObjectResult(new { error = "Email service is not configured properly." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            // Configure Brevo client
            Configuration.Default.ApiKey["api-key"] = apiKey;
            var apiInstance = new TransactionalEmailsApi();

            // Build email using IOptions configuration
            var sender = new SendSmtpEmailSender { Email = _emailSettings.FromEmail };
            var recipient = new SendSmtpEmailTo(_emailSettings.FromEmail, InputValidator.SanitizeHtml(emailRequest.FromName));

            var ccList = new List<SendSmtpEmailCc>();
            if (!string.IsNullOrEmpty(_emailSettings.CcEmail))
            {
                ccList.Add(new SendSmtpEmailCc(_emailSettings.CcEmail));
            }

            var email = new SendSmtpEmail(
                sender: sender,
                to: new List<SendSmtpEmailTo> { recipient },
                cc: ccList.Count > 0 ? ccList : null,
                subject: InputValidator.SanitizeHtml(emailRequest.Subject),
                htmlContent: BuildHtmlContent(emailRequest),
                textContent: BuildTextContent(emailRequest)
            );

            // Send email
            var response = await apiInstance.SendTransacEmailAsync(email);

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", response.MessageId);

            return new OkObjectResult(new
            {
                success = true,
                message = "Email sent successfully.",
                messageId = response.MessageId
            });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Brevo API error: {Message}", ex.Message);
            return new ObjectResult(new { error = "Failed to send email. Please try again later." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error: {Message}", ex.Message);
            return new BadRequestObjectResult(new { error = "Invalid request format." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email: {Message}", ex.Message);
            return new ObjectResult(new { error = "An unexpected error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <summary>
    /// Validates all fields of the email request for security and business rules.
    /// </summary>
    /// <param name="request">The <see cref="EmailRequest"/> to validate.</param>
    /// <returns>
    /// An error message string if validation fails; otherwise, <c>null</c> if all validations pass.
    /// </returns>
    /// <remarks>
    /// Validation includes:
    /// <list type="bullet">
    ///   <item><description>Name: Required, max 100 characters, no dangerous content</description></item>
    ///   <item><description>Email: Required, valid format, max 254 characters</description></item>
    ///   <item><description>Subject: Required, max 200 characters, no dangerous content</description></item>
    ///   <item><description>Message: Required, max 5000 characters, no dangerous content</description></item>
    /// </list>
    /// All validations use <see cref="InputValidator"/> for security checks.
    /// </remarks>
    private static string? ValidateEmailRequest(EmailRequest request)
    {
        // Validate name
        var nameValidation = InputValidator.ValidateTextInput(request.FromName, "Name", maxLength: 100);
        if (!nameValidation.IsValid)
            return nameValidation.ErrorMessage;

        // Validate email
        var emailValidation = InputValidator.ValidateEmail(request.FromEmail);
        if (!emailValidation.IsValid)
            return emailValidation.ErrorMessage;

        // Validate subject
        var subjectValidation = InputValidator.ValidateTextInput(request.Subject, "Subject", maxLength: 200);
        if (!subjectValidation.IsValid)
            return subjectValidation.ErrorMessage;

        // Validate message
        var messageValidation = InputValidator.ValidateTextInput(request.Message, "Message", maxLength: 5000);
        if (!messageValidation.IsValid)
            return messageValidation.ErrorMessage;

        return null;
    }

    /// <summary>
    /// Builds the HTML content for the email body with a styled template.
    /// </summary>
    /// <param name="request">The <see cref="EmailRequest"/> containing the email data.</param>
    /// <returns>
    /// A formatted HTML string containing the email content with inline styles
    /// for consistent rendering across email clients.
    /// </returns>
    /// <remarks>
    /// All user-provided content is sanitized using <see cref="InputValidator.SanitizeHtml"/>
    /// before being embedded in the HTML to prevent XSS attacks. Newlines in the message
    /// are converted to <c>&lt;br/&gt;</c> tags for proper display.
    /// </remarks>
    private static string BuildHtmlContent(EmailRequest request)
    {
        // All content is sanitized before being embedded in HTML
        var safeName = InputValidator.SanitizeHtml(request.FromName);
        var safeEmail = InputValidator.SanitizeHtml(request.FromEmail);
        var safeSubject = InputValidator.SanitizeHtml(request.Subject);
        var safeMessage = InputValidator.SanitizeHtml(request.Message).Replace("\n", "<br/>");

        return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #4F46E5;'>New Contact Form Submission</h2>
                <hr style='border: 1px solid #E5E7EB;' />
                <p><strong>From:</strong> {safeName}</p>
                <p><strong>Email:</strong> {safeEmail}</p>
                <p><strong>Subject:</strong> {safeSubject}</p>
                <hr style='border: 1px solid #E5E7EB;' />
                <h3 style='color: #374151;'>Message:</h3>
                <div style='background-color: #F9FAFB; padding: 15px; border-radius: 8px;'>
                    {safeMessage}
                </div>
                <hr style='border: 1px solid #E5E7EB;' />
                <p style='color: #6B7280; font-size: 12px;'>
                    This email was sent from the CloudZen contact form.
                </p>
            </div>";
    }

    /// <summary>
    /// Builds the plain text content for the email body as a fallback for non-HTML email clients.
    /// </summary>
    /// <param name="request">The <see cref="EmailRequest"/> containing the email data.</param>
    /// <returns>
    /// A formatted plain text string containing the email content with ASCII formatting
    /// for readability in text-only email clients.
    /// </returns>
    /// <remarks>
    /// This plain text version is sent alongside the HTML version to ensure compatibility
    /// with all email clients. Content is used as-is without HTML encoding since this
    /// is plain text output.
    /// </remarks>
    private static string BuildTextContent(EmailRequest request)
    {
        return $@"New Contact Form Submission
=============================
From: {request.FromName}
Email: {request.FromEmail}
Subject: {request.Subject}

Message:
{request.Message}

-----------------------------
This email was sent from the CloudZen contact form.";
    }
}
