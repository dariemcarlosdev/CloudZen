using CloudZen.Api.Models;
using CloudZen.Api.Security;
using CloudZen.Api.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Security.Authentication;
using System.Text.Json;

namespace CloudZen.Api.Functions;

/// <summary>
/// Azure Function to handle email sending through Brevo SMTP relay.
/// </summary>
/// <remarks>
/// This function serves as a secure backend for the Blazor WebAssembly frontend contact form,
/// ensuring that API keys remain secure on the server side and are never exposed to the client.
/// <para>
/// Uses SMTP instead of REST API to avoid IP whitelisting issues with Azure Functions Consumption plan.
/// </para>
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
/// <param name="logger">The logger instance for diagnostic output.</param>
/// <param name="config">The configuration provider for accessing secrets (API keys).</param>
/// <param name="rateLimiter">The rate limiter service for throttling requests.</param>
/// <param name="corsSettings">The CORS settings for cross-origin requests.</param>
/// <param name="emailSettings">The email configuration settings.</param>
public class SendEmailFunction(
    ILogger<SendEmailFunction> logger,
    IConfiguration config,
    IRateLimiterService rateLimiter,
    CorsSettings corsSettings,
    IOptions<EmailSettings> emailSettings)
{
    private readonly ILogger<SendEmailFunction> _logger = logger;
    private readonly IConfiguration _config = config;
    private readonly IRateLimiterService _rateLimiter = rateLimiter;
    private readonly CorsSettings _corsSettings = corsSettings;
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    // Brevo SMTP settings
    private const string BrevoSmtpHost = "smtp-relay.brevo.com";
    private const int BrevoSmtpPort = 587;

    // Cached JsonSerializerOptions for email request deserialization
    private static readonly JsonSerializerOptions EmailRequestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 10 // Prevent deep object attacks
    };

    /// <summary>
    /// HTTP POST endpoint to send emails via Brevo SMTP relay.
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
    ///   <item><description><b>500 Internal Server Error</b> - Email service configuration error or SMTP failure</description></item>
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

            var emailRequest = JsonSerializer.Deserialize<EmailRequest>(requestBody, EmailRequestJsonOptions);

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

            // Get Brevo SMTP credentials from configuration
            var smtpLogin = _config["BREVO_SMTP_LOGIN"] ?? Environment.GetEnvironmentVariable("BREVO_SMTP_LOGIN");
            var smtpKey = _config["BREVO_SMTP_KEY"] ?? Environment.GetEnvironmentVariable("BREVO_SMTP_KEY");

            // Fall back to API key if SMTP key not set (Brevo allows using API key as SMTP password)
            if (string.IsNullOrEmpty(smtpKey))
            {
                smtpKey = _config["BREVO_API_KEY"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");
            }

            if (string.IsNullOrEmpty(smtpLogin) || string.IsNullOrEmpty(smtpKey))
            {
                _logger.LogError("Brevo SMTP credentials are not configured. Ensure BREVO_SMTP_LOGIN and BREVO_SMTP_KEY (or BREVO_API_KEY) are set.");
                return new ObjectResult(new { error = "Email service is not configured properly." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            // Build and send email via SMTP
            var messageId = await SendEmailViaSmtpAsync(emailRequest, smtpLogin, smtpKey);

            _logger.LogInformation("Email sent successfully via SMTP. MessageId: {MessageId}", messageId);

            return new OkObjectResult(new
            {
                success = true,
                message = "Email sent successfully.",
                messageId
            });
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(ex, "SMTP command error: {Message}, StatusCode: {StatusCode}", ex.Message, ex.StatusCode);
            return new ObjectResult(new { error = "Failed to send email. Please try again later." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(ex, "SMTP protocol error: {Message}", ex.Message);
            return new ObjectResult(new { error = "Failed to send email. Please try again later." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (System.Security.Authentication.AuthenticationException ex)
        {
            _logger.LogError(ex, "SMTP authentication error: {Message}", ex.Message);
            return new ObjectResult(new { error = "Email service configuration error." })
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
    /// Sends an email using Brevo SMTP relay with MailKit.
    /// </summary>
    /// <param name="emailRequest">The email request containing message details.</param>
    /// <param name="smtpLogin">The SMTP login (typically your Brevo account email).</param>
    /// <param name="smtpKey">The SMTP key (your Brevo SMTP password or API key).</param>
    /// <returns>The message ID of the sent email.</returns>
    private async Task<string> SendEmailViaSmtpAsync(EmailRequest emailRequest, string smtpLogin, string smtpKey)
    {
        var message = new MimeMessage();

        // Set sender
        message.From.Add(new MailboxAddress(_emailSettings.FromName ?? "CloudZen Contact", _emailSettings.FromEmail));

        // Set recipient (contact form sends to yourself)
        message.To.Add(new MailboxAddress(InputValidator.SanitizeHtml(emailRequest.FromName), _emailSettings.FromEmail));

        // Add CC if configured
        if (!string.IsNullOrEmpty(_emailSettings.CcEmail))
        {
            message.Cc.Add(new MailboxAddress("", _emailSettings.CcEmail));
        }

        // Set Reply-To as the sender from the form
        message.ReplyTo.Add(new MailboxAddress(InputValidator.SanitizeHtml(emailRequest.FromName), emailRequest.FromEmail));

        // Set subject
        message.Subject = InputValidator.SanitizeHtml(emailRequest.Subject);

        // Build multipart body with both HTML and plain text
        var builder = new BodyBuilder
        {
            HtmlBody = BuildHtmlContent(emailRequest),
            TextBody = BuildTextContent(emailRequest)
        };

        message.Body = builder.ToMessageBody();

        // Generate a unique message ID
        var messageId = $"{Guid.NewGuid():N}@cloudzen.com";
        message.MessageId = messageId;

        // Send via SMTP
        using var client = new SmtpClient();

        // IMPORTANT: Disable certificate revocation check BEFORE setting the callback
        // This is required because revocation servers may be unreachable in some networks
        client.CheckCertificateRevocation = false;

        // Configure certificate validation for Brevo's SMTP server.
        // CheckCertificateRevocation is already disabled above, so revocation-related chain
        // errors are expected and acceptable. All other errors (name mismatch, untrusted root,
        // etc.) are rejected to prevent MITM attacks (OWASP A02: Cryptographic Failures).
        client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                return true;

            // Allow chain errors caused only by revocation check being disabled
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors &&
                chain != null &&
                chain.ChainStatus.All(status =>
                    status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.RevocationStatusUnknown ||
                    status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.OfflineRevocation))
            {
                return true;
            }

            _logger.LogError("SSL certificate validation failed for Brevo SMTP. Errors: {Errors}", sslPolicyErrors);
            return false;
        };

        // Connect with STARTTLS
        await client.ConnectAsync(BrevoSmtpHost, BrevoSmtpPort, SecureSocketOptions.StartTls);

        // Authenticate
        await client.AuthenticateAsync(smtpLogin, smtpKey);

        // Send
        await client.SendAsync(message);

        // Disconnect
        await client.DisconnectAsync(true);

        return messageId;
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
