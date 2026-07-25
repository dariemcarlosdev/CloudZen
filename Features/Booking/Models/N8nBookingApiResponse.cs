namespace CloudZen.Features.Booking.Models;

/// <summary>
/// Maps the raw JSON response from the N8N booking workflow.
/// Internal DTO used by <see cref="Services.AppointmentService"/> for deserialization.
/// </summary>
public sealed record N8nBookingApiResponse
{
    /// <summary>Whether the N8N workflow operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>The action that was performed (book, cancel, reschedule).</summary>
    public string? Action { get; init; }

    /// <summary>The booking confirmation ID (e.g. "APT-MN7O3825-TMVP").</summary>
    public string? BookingId { get; init; }

    /// <summary>Human-readable message from the N8N workflow.</summary>
    public string? Message { get; init; }
}
