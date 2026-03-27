using System.Net.Http.Json;
using System.Text.Json;
using CloudZen.Features.Booking.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudZen.Features.Booking.Services;

/// <summary>
/// Sends appointment requests (book, cancel, reschedule) through the Azure Functions proxy endpoint.
/// </summary>
/// <remarks>
/// The WASM client cannot call the n8n webhook directly due to CORS restrictions.
/// Requests are sent to <c>/api/book-appointment</c> (Azure Functions),
/// which forwards them to n8n server-to-server.
/// </remarks>
public class AppointmentService : IAppointmentService
{
    private readonly HttpClient _httpClient;
    private readonly BookingServiceOptions _options;
    private readonly ILogger<AppointmentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppointmentService(
        HttpClient httpClient,
        IOptions<BookingServiceOptions> options,
        ILogger<AppointmentService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<AppointmentResponse> BookAsync(BookAppointmentRequest request)
    {
        _logger.LogInformation("Booking appointment for {Email} on {Date} at {Time}",
            request.Email, request.Date, request.Time);

        return await SendAsync(request, "book");
    }

    /// <inheritdoc />
    public async Task<AppointmentResponse> CancelAsync(CancelAppointmentRequest request)
    {
        _logger.LogInformation("Cancelling appointment {BookingId} for {Email}",
            request.BookingId, request.Email);

        return await SendAsync(request, "cancel");
    }

    /// <inheritdoc />
    public async Task<AppointmentResponse> RescheduleAsync(RescheduleAppointmentRequest request)
    {
        _logger.LogInformation("Rescheduling appointment {BookingId} to {NewDate} at {NewTime}",
            request.BookingId, request.NewDate, request.NewTime);

        return await SendAsync(request, "reschedule");
    }

    /// <summary>
    /// Sends a request to the API and maps the response.
    /// </summary>
    private async Task<AppointmentResponse> SendAsync<TRequest>(TRequest request, string action)
        where TRequest : class
    {
        try
        {
            var endpoint = _options.BookAppointmentUrl;
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            var statusCode = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("{Action} API response {StatusCode}: {Body}", action, statusCode, body);

            // Handle empty response body
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("{Action} received empty response with status {StatusCode}", action, statusCode);

                if (response.IsSuccessStatusCode && action != "book")
                {
                    return AppointmentResponse.Ok(statusCode, action, "Operation completed successfully.");
                }

                return AppointmentResponse.Fail(statusCode, "We received an empty response from the server.");
            }

            var apiResponse = JsonSerializer.Deserialize<N8nBookingApiResponse>(body, JsonOptions);

            if (apiResponse is null)
            {
                return AppointmentResponse.Fail(statusCode, "We received an unexpected response format.");
            }

            return MapToAppointmentResponse(apiResponse, statusCode, action);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during {Action}: {Message}", action, ex.Message);
            return AppointmentResponse.NetworkError(
                "Our booking system is temporarily unreachable. Please try again in a moment.");
        }
        catch (TaskCanceledException ex)
            when (ex.InnerException is TimeoutException || !ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout during {Action} after {Seconds}s", action, _options.TimeoutSeconds);
            return AppointmentResponse.NetworkError("The request took too long. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {Action}: {Message}", action, ex.Message);
            return AppointmentResponse.Fail(500, "Something went wrong. Please try again later.");
        }
    }

    /// <summary>
    /// Maps the API response to an <see cref="AppointmentResponse"/>.
    /// </summary>
    private static AppointmentResponse MapToAppointmentResponse(N8nBookingApiResponse api, int statusCode, string action)
    {
        if (api.Success)
        {
            return action == "book"
                ? AppointmentResponse.Confirmed(statusCode, api.BookingId ?? "N/A", api.Message)
                : AppointmentResponse.Ok(statusCode, action, api.Message);
        }

        var error = api.Message ?? "The operation could not be completed.";

        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            return AppointmentResponse.NotFound(statusCode, error);
        }

        if (error.Contains("already booked", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("slot", StringComparison.OrdinalIgnoreCase))
        {
            return AppointmentResponse.SlotTaken(statusCode, error);
        }

        return AppointmentResponse.Fail(statusCode, error);
    }
}
