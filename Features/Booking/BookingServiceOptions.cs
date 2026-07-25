namespace CloudZen.Features.Booking;

/// <summary>
/// Configuration options for the appointment booking API endpoint.
/// Bound from the <c>"BookingService"</c> section of <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// The frontend calls the Azure Functions proxy at <c>/api/book-appointment</c>,
/// which then forwards to the n8n webhook server-side (avoiding CORS issues).
/// </para>
/// <para>
/// In local development, <c>ApiBaseUrl</c> is overridden in <c>Program.cs</c> to point
/// to the local Functions host (e.g. <c>"http://localhost:7257/api"</c>).
/// </para>
/// </remarks>
public class BookingServiceOptions
{
    /// <summary>
    /// The configuration section name used to bind these options from <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "BookingService";

    /// <summary>
    /// Gets or sets the base URL for the booking API backend.
    /// Defaults to <c>"/api"</c> for Azure Static Web Apps linked functions.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "/api";

    /// <summary>
    /// Gets or sets the booking endpoint path (appended to <see cref="ApiBaseUrl"/>).
    /// </summary>
    public string BookEndpoint { get; set; } = "book-appointment";

    /// <summary>
    /// HTTP request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the full URL for the book-appointment endpoint.
    /// </summary>
    public string BookAppointmentUrl => $"{ApiBaseUrl.TrimEnd('/')}/{BookEndpoint}";
}
