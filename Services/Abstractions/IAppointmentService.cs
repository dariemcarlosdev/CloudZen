using CloudZen.Models;

namespace CloudZen.Services.Abstractions;

/// <summary>
/// Result of a booking appointment operation against the n8n webhook.
/// Uses factory methods instead of throwing exceptions.
/// </summary>
public class BookingResult
{
    /// <summary>Indicates whether the booking was successfully confirmed.</summary>
    public bool Success { get; set; }

    /// <summary>
    /// The unique booking confirmation ID returned by the n8n workflow
    /// (e.g. <c>"APT-MN7O3825-TMVP"</c>). Only populated on success.
    /// </summary>
    public string? BookingId { get; set; }

    /// <summary>Human-readable confirmation or informational message from the workflow.</summary>
    public string? Message { get; set; }

    /// <summary>Human-readable error description when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates the failure was caused by a scheduling conflict (time slot already booked).
    /// When <c>true</c>, the UI should offer the user a way to pick a different time.
    /// </summary>
    public bool IsSlotTaken { get; set; }

    /// <summary>Slot was free and the appointment was confirmed.</summary>
    public static BookingResult Confirmed(string bookingId, string? message = null) =>
        new() { Success = true, BookingId = bookingId, Message = message };

    /// <summary>Slot was already taken — user should pick a different time.</summary>
    public static BookingResult SlotTaken(string message) =>
        new() { Success = false, Error = message, IsSlotTaken = true };

    /// <summary>General failure (network, timeout, unexpected).</summary>
    public static BookingResult Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>
/// Sends appointment booking requests to the n8n webhook endpoint.
/// </summary>
public interface IAppointmentService
{
    /// <summary>
    /// Books an appointment via the n8n workflow.
    /// </summary>
    /// <param name="request">The appointment details matching the n8n JSON contract.</param>
    /// <returns>
    /// A <see cref="BookingResult"/> indicating confirmed, slot-taken, or failure.
    /// </returns>
    Task<BookingResult> BookAppointmentAsync(BookingAppointmentRequest request);
}
