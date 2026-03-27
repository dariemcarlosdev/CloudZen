using CloudZen.Api.Shared.Security;
using CloudZen.Api.Shared.Services;
using CloudZen.Api.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CloudZen.Api.Features.Booking;

/// <summary>
/// Azure Function that proxies appointment booking requests to the n8n webhook.
/// </summary>
/// <remarks>
/// <para>
/// This function serves as a secure backend proxy for the Blazor WebAssembly booking flow,
/// forwarding requests to the n8n appointment workflow at a configured webhook URL.
/// The n8n webhook cannot be called directly from the browser due to CORS restrictions.
/// </para>
/// <para>
/// Security features:
/// <list type="bullet">
///   <item><description>Rate limiting to prevent abuse</description></item>
///   <item><description>Input validation and sanitization</description></item>
///   <item><description>CORS and security headers</description></item>
///   <item><description>Request body size limiting</description></item>
///   <item><description>Correlation ID tracking</description></item>
/// </list>
/// </para>
/// </remarks>
public class BookAppointmentFunction(
    ILogger<BookAppointmentFunction> logger,
    IConfiguration config,
    IRateLimiterService rateLimiter,
    CorsSettings corsSettings,
    IHttpClientFactory httpClientFactory)
{
    private readonly ILogger<BookAppointmentFunction> _logger = logger;
    private readonly IConfiguration _config = config;
    private readonly IRateLimiterService _rateLimiter = rateLimiter;
    private readonly CorsSettings _corsSettings = corsSettings;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private const int MaxRequestBodySize = 5000;

    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 10
    };

    /// <summary>
    /// HTTP POST endpoint to book an appointment via the n8n webhook.
    /// Also handles OPTIONS preflight requests for CORS.
    /// </summary>
    /// <param name="req">The HTTP request containing a <see cref="BookAppointmentRequest"/> JSON body.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing:
    /// <list type="bullet">
    ///   <item><description><b>200 OK</b> — Booking confirmed with bookingId</description></item>
    ///   <item><description><b>200 OK</b> — Slot taken (success=false in body)</description></item>
    ///   <item><description><b>204 No Content</b> — CORS preflight</description></item>
    ///   <item><description><b>400 Bad Request</b> — Validation failure</description></item>
    ///   <item><description><b>429 Too Many Requests</b> — Rate limit exceeded</description></item>
    ///   <item><description><b>502 Bad Gateway</b> — n8n webhook unreachable</description></item>
    /// </list>
    /// </returns>
    [Function("BookAppointment")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "book-appointment")] HttpRequest req)
    {
        // ── CORS ─────────────────────────────────────────────────────────
        req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);

        if (req.IsCorsPreflightRequest())
        {
            return new StatusCodeResult(StatusCodes.Status204NoContent);
        }

        req.HttpContext.Response.AddSecurityHeaders();

        // ── Logging / Rate limiting ──────────────────────────────────────
        var clientIp = req.GetClientIpAddress();
        var correlationId = req.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ClientIp"] = InputValidator.SanitizeForLogging(clientIp)
        });

        _logger.LogInformation("BookAppointment triggered from {ClientIp}", InputValidator.SanitizeForLogging(clientIp));

        try
        {
            // Rate limit
            var rateLimitResult = await _rateLimiter.TryAcquireAsync(clientIp, "book-appointment");
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for {ClientIp}", InputValidator.SanitizeForLogging(clientIp));
                req.HttpContext.Response.Headers.TryAdd("Retry-After",
                    rateLimitResult.RetryAfter?.TotalSeconds.ToString("F0") ?? "60");

                return new ObjectResult(new { success = false, message = rateLimitResult.Message })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
            }

            // ── Parse & validate ─────────────────────────────────────────
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            _logger.LogInformation("Received request body: {Body}", requestBody);

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult(new { success = false, message = "Please fill out all required fields and try again." });
            }

            if (requestBody.Length > MaxRequestBodySize)
            {
                return new BadRequestObjectResult(new { success = false, message = "Your request contains too much data. Please shorten your entries and try again." });
            }

            var bookingRequest = JsonSerializer.Deserialize<BookAppointmentRequest>(requestBody, RequestJsonOptions);
            if (bookingRequest is null)
            {
                return new BadRequestObjectResult(new { success = false, message = "We couldn't read your booking details. Please try again." });
            }

            _logger.LogInformation(
                "Parsed request - Action: {Action}, Name: {Name}, Email: {Email}, Date: {Date}, Time: {Time}",
                bookingRequest.Action,
                bookingRequest.Name,
                bookingRequest.Email,
                bookingRequest.Date,
                bookingRequest.Time);

            var validationError = ValidateRequest(bookingRequest);
            if (validationError is not null)
            {
                _logger.LogWarning("Validation failed: {Error}", validationError);
                return new BadRequestObjectResult(new { success = false, message = validationError });
            }

            // ── Forward to n8n webhook ───────────────────────────────────
            // N8N's "Prepare Base Data" node handles field transformation internally,
            // so we send the original request payload directly.
            var webhookUrl = _config["N8N_WEBHOOK_URL"]
                          ?? Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL");

            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogError("N8N_WEBHOOK_URL is not configured.");
                return new ObjectResult(new { success = false, message = "Our booking system is temporarily unavailable. Please try again later." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            var httpClient = _httpClientFactory.CreateClient("SecureClient");

            // Send original request body - N8N JavaScript handles the transformation
            var jsonContent = new StringContent(
                requestBody,
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Forwarding {Action} request to n8n for {Email}",
                bookingRequest.Action,
                InputValidator.SanitizeForLogging(bookingRequest.Email));

            var n8nResponse = await httpClient.PostAsync(webhookUrl, jsonContent);
            var n8nBody = await n8nResponse.Content.ReadAsStringAsync();

            _logger.LogDebug("n8n response {StatusCode}: {Body}", n8nResponse.StatusCode, n8nBody);

            if (!n8nResponse.IsSuccessStatusCode)
            {
                _logger.LogError("n8n returned {StatusCode}: {Body}", n8nResponse.StatusCode, n8nBody);
                return new ObjectResult(new { success = false, message = "We couldn't complete your booking right now. Please try again." })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }

            // Pass the n8n JSON response through to the frontend as-is
            // (it already contains { success, bookingId, message } or { success: false, message })
            return new ContentResult
            {
                Content = n8nBody,
                ContentType = "application/json",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error reaching n8n webhook: {Message}", ex.Message);
            return new ObjectResult(new { success = false, message = "Our booking system is temporarily unreachable. Please try again in a moment." })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
        catch (TaskCanceledException ex)
            when (ex.InnerException is TimeoutException || !ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout reaching n8n webhook");
            return new ObjectResult(new { success = false, message = "The request took too long. Please try again." })
            {
                StatusCode = StatusCodes.Status504GatewayTimeout
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in BookAppointment: {Message}", ex.Message);
            return new ObjectResult(new { success = false, message = "Something went wrong. Please try again later." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <summary>
    /// Validates request fields based on the action type.
    /// </summary>
    /// <returns>An error message string, or <c>null</c> if valid.</returns>
    private static string? ValidateRequest(BookAppointmentRequest request)
    {
        // Validate action
        var validActions = new[] { "book", "cancel", "reschedule" };
        if (!validActions.Contains(request.Action.ToLowerInvariant()))
        {
            return "Invalid action. Must be 'book', 'cancel', or 'reschedule'.";
        }

        // Email is always required
        var emailResult = InputValidator.ValidateEmail(request.Email);
        if (!emailResult.IsValid) return emailResult.ErrorMessage;

        return request.Action.ToLowerInvariant() switch
        {
            "book" => ValidateBookAction(request),
            "cancel" => ValidateCancelAction(request),
            "reschedule" => ValidateRescheduleAction(request),
            _ => "Invalid action."
        };
    }

    /// <summary>
    /// Validates fields required for the "book" action.
    /// </summary>
    private static string? ValidateBookAction(BookAppointmentRequest request)
    {
        var nameResult = InputValidator.ValidateTextInput(request.Name, "Name", maxLength: 100);
        if (!nameResult.IsValid) return nameResult.ErrorMessage;

        var phoneResult = InputValidator.ValidateTextInput(request.Phone, "Phone", maxLength: 20);
        if (!phoneResult.IsValid) return phoneResult.ErrorMessage;

        var businessResult = InputValidator.ValidateTextInput(request.BusinessName, "Business Name", maxLength: 200);
        if (!businessResult.IsValid) return businessResult.ErrorMessage;

        var dateResult = InputValidator.ValidateTextInput(request.Date, "Date", maxLength: 10);
        if (!dateResult.IsValid) return dateResult.ErrorMessage;

        var timeResult = InputValidator.ValidateTextInput(request.Time, "Time", maxLength: 5);
        if (!timeResult.IsValid) return timeResult.ErrorMessage;

        var endTimeResult = InputValidator.ValidateTextInput(request.EndTime, "End Time", maxLength: 5);
        if (!endTimeResult.IsValid) return endTimeResult.ErrorMessage;

        // Validate date format (YYYY-MM-DD)
        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", out _))
            return "Please select a valid date.";

        // Validate time format (HH:mm)
        if (!TimeOnly.TryParseExact(request.Time, "HH:mm", out _))
            return "Please select a valid time slot.";

        if (!TimeOnly.TryParseExact(request.EndTime, "HH:mm", out _))
            return "Please select a valid time slot.";

        // Validate phone starts with +
        if (!request.Phone.StartsWith('+'))
            return "Please enter a valid phone number with country code.";

        return null;
    }

    /// <summary>
    /// Validates fields required for the "cancel" action.
    /// </summary>
    private static string? ValidateCancelAction(BookAppointmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BookingId))
            return "Booking ID is required to cancel an appointment.";

        // BookingId format: APT-XXXXXXXX-XXXX
        if (!request.BookingId.StartsWith("APT-") || request.BookingId.Length < 10)
            return "Please enter a valid booking ID (e.g., APT-MN7O3825-TMVP).";

        return null;
    }

    /// <summary>
    /// Validates fields required for the "reschedule" action.
    /// </summary>
    private static string? ValidateRescheduleAction(BookAppointmentRequest request)
    {
        // First validate cancel fields (bookingId)
        var cancelValidation = ValidateCancelAction(request);
        if (cancelValidation is not null) return cancelValidation;

        // Then validate new date/time
        if (string.IsNullOrWhiteSpace(request.NewDate))
            return "New date is required for rescheduling.";

        if (string.IsNullOrWhiteSpace(request.NewTime))
            return "New time is required for rescheduling.";

        if (string.IsNullOrWhiteSpace(request.NewEndTime))
            return "New end time is required for rescheduling.";

        if (!DateOnly.TryParseExact(request.NewDate, "yyyy-MM-dd", out _))
            return "Please select a valid new date.";

        if (!TimeOnly.TryParseExact(request.NewTime, "HH:mm", out _))
            return "Please select a valid new time slot.";

        if (!TimeOnly.TryParseExact(request.NewEndTime, "HH:mm", out _))
            return "Please select a valid new time slot.";

        return null;
    }
}
