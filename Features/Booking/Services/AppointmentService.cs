using System.Net.Http.Json;
using System.Text.Json;
using CloudZen.Features.Booking.Models;
using CloudZen.Features.Booking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudZen.Features.Booking.Services;

/// <summary>
/// Sends appointment booking requests through the Azure Functions proxy endpoint.
/// Follows the same HttpClient / IOptions / ILogger pattern as <see cref="ApiEmailService"/>.
/// </summary>
/// <remarks>
/// The WASM client cannot call the n8n webhook directly due to CORS restrictions.
/// Instead, requests are sent to <c>/api/book-appointment</c> (Azure Functions),
/// which forwards them to n8n server-to-server.
/// </remarks>
public class AppointmentService : IAppointmentService
{
    /// <summary>HTTP client used to POST booking requests to the Azure Functions proxy.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Strongly-typed configuration for the API endpoint URL and timeout.</summary>
    private readonly BookingServiceOptions _options;

    /// <summary>Logger for diagnostic and error output.</summary>
    private readonly ILogger<AppointmentService> _logger;

    /// <summary>Shared JSON serializer options with case-insensitive property matching.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AppointmentService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to communicate with the API backend.</param>
    /// <param name="options">The booking service configuration options.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/>, <paramref name="options"/>,
    /// or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
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
    public async Task<BookingResult> BookAppointmentAsync(BookingAppointmentRequest request)
    {
        try
        {
            var endpoint = _options.BookAppointmentUrl;
            _logger.LogInformation(
                "Booking appointment for {Name} on {Date} at {Time} via {Endpoint}",
                request.Name, request.Date, request.Time, endpoint);

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Booking API response {StatusCode}: {Body}", response.StatusCode, body);

            // The Azure Function proxies the n8n JSON payload on 200.
            // On 4xx/5xx, the body also contains { success, message }.
            var apiResponse = JsonSerializer.Deserialize<BookingApiResponse>(body, JsonOptions);

            if (apiResponse is null)
            {
                return BookingResult.Fail("We received an unexpected response. Please try again.");
            }

            if (apiResponse.Success)
            {
                _logger.LogInformation("Appointment confirmed. BookingId: {BookingId}", apiResponse.BookingId);
                return BookingResult.Confirmed(
                    apiResponse.BookingId ?? "N/A",
                    apiResponse.Message);
            }
            else
            {
                // Slot taken, validation error, or upstream failure
                _logger.LogWarning("Booking not confirmed: {Message}", apiResponse.Message);
                return BookingResult.SlotTaken(
                    apiResponse.Message ?? "This time slot is already booked. Please choose a different time.");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error booking appointment: {Message}", ex.Message);
            return BookingResult.Fail(
                "Our booking system is temporarily unreachable. Please try again in a moment.");
        }
        catch (TaskCanceledException ex)
            when (ex.InnerException is TimeoutException || !ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout booking appointment after {Seconds}s", _options.TimeoutSeconds);
            return BookingResult.Fail("The request took too long. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error booking appointment: {Message}", ex.Message);
            return BookingResult.Fail("Something went wrong. Please try again later.");
        }
    }

    /// <summary>
    /// Maps the JSON response from the booking API (Azure Functions proxy).
    /// </summary>
    /// <remarks>
    /// On success the Azure Function passes through the n8n response as-is.
    /// On failure the Function or n8n returns <c>{ success: false, message: "..." }</c>.
    /// </remarks>
    private sealed class BookingApiResponse
    {
        /// <summary>Whether the booking was successfully created.</summary>
        public bool Success { get; set; }

        /// <summary>The workflow action echoed back (e.g. <c>"book"</c>).</summary>
        public string? Action { get; set; }

        /// <summary>The unique booking confirmation ID (e.g. <c>"APT-MN7O3825-TMVP"</c>).</summary>
        public string? BookingId { get; set; }

        /// <summary>Human-readable message from the workflow or API.</summary>
        public string? Message { get; set; }
    }
}
