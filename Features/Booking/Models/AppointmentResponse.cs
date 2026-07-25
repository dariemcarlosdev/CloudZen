namespace CloudZen.Features.Booking.Models;

/// <summary>
/// Unified response for all appointment operations (book, cancel, reschedule).
/// Includes HTTP status code from the N8N workflow response.
/// </summary>
public sealed class AppointmentResponse
{
    /// <summary>HTTP status code from the API/N8N response.</summary>
    public int StatusCode { get; init; }

    /// <summary>Indicates whether the operation was successful.</summary>
    public bool Success { get; init; }

    /// <summary>
    /// The unique booking confirmation ID (e.g. <c>"APT-MN7O3825-TMVP"</c>).
    /// Populated on successful book operations.
    /// </summary>
    public string? BookingId { get; init; }

    /// <summary>Human-readable message from the workflow.</summary>
    public string? Message { get; init; }

    /// <summary>Human-readable error description when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Error { get; init; }

    /// <summary>The action that was performed (book, cancel, reschedule).</summary>
    public string? Action { get; init; }

    /// <summary>
    /// Failure was caused by a scheduling conflict (time slot already booked).
    /// When <c>true</c>, the UI should offer the user a way to pick a different time.
    /// </summary>
    public bool IsSlotTaken { get; init; }

    /// <summary>
    /// Booking was not found (for cancel/reschedule operations).
    /// </summary>
    public bool IsNotFound { get; init; }

    /// <summary>Indicates a network or timeout error occurred.</summary>
    public bool IsNetworkError { get; init; }

    // ── Factory Methods ──────────────────────────────────────────────────

    /// <summary>Creates a successful booking confirmation response.</summary>
    public static AppointmentResponse Confirmed(int statusCode, string bookingId, string? message = null) => new()
    {
        StatusCode = statusCode,
        Success = true,
        BookingId = bookingId,
        Message = message,
        Action = "book"
    };

    /// <summary>Creates a successful cancel/reschedule response.</summary>
    public static AppointmentResponse Ok(int statusCode, string action, string? message = null) => new()
    {
        StatusCode = statusCode,
        Success = true,
        Message = message,
        Action = action
    };

    /// <summary>Creates a slot-taken failure response.</summary>
    public static AppointmentResponse SlotTaken(int statusCode, string error) => new()
    {
        StatusCode = statusCode,
        Success = false,
        Error = error,
        IsSlotTaken = true
    };

    /// <summary>Creates a not-found failure response.</summary>
    public static AppointmentResponse NotFound(int statusCode, string error) => new()
    {
        StatusCode = statusCode,
        Success = false,
        Error = error,
        IsNotFound = true
    };

    /// <summary>Creates a network/timeout error response.</summary>
    public static AppointmentResponse NetworkError(string error) => new()
    {
        StatusCode = 0,
        Success = false,
        Error = error,
        IsNetworkError = true
    };

    /// <summary>Creates a general failure response.</summary>
    public static AppointmentResponse Fail(int statusCode, string error) => new()
    {
        StatusCode = statusCode,
        Success = false,
        Error = error
    };
}
