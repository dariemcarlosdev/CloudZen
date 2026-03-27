using System.Text.Json.Serialization;

namespace CloudZen.Features.Booking.Models;

/// <summary>
/// Request to book a new appointment.
/// </summary>
public sealed record BookAppointmentRequest
{
    /// <summary>Full name of the person booking the appointment.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Email address for calendar invites and confirmations.</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>Phone in E.164 format (e.g. "+15551234567").</summary>
    [JsonPropertyName("phone")]
    public required string Phone { get; init; }

    /// <summary>Name of the business or organization.</summary>
    [JsonPropertyName("businessName")]
    public required string BusinessName { get; init; }

    /// <summary>Appointment date in YYYY-MM-DD format.</summary>
    [JsonPropertyName("date")]
    public required string Date { get; init; }

    /// <summary>Start time in HH:mm 24-hour format.</summary>
    [JsonPropertyName("time")]
    public required string Time { get; init; }

    /// <summary>End time in HH:mm 24-hour format.</summary>
    [JsonPropertyName("endTime")]
    public required string EndTime { get; init; }

    /// <summary>Reason for the appointment.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "CloudZen Meeting Request";

    /// <summary>Workflow action (always "book" for this request type).</summary>
    [JsonPropertyName("action")]
    public string Action => "book";
}

/// <summary>
/// Request to cancel an existing appointment.
/// </summary>
public sealed record CancelAppointmentRequest
{
    /// <summary>The booking ID to cancel (e.g. "APT-MN7O3825-TMVP").</summary>
    [JsonPropertyName("bookingId")]
    public required string BookingId { get; init; }

    /// <summary>Email address associated with the booking.</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>Workflow action (always "cancel" for this request type).</summary>
    [JsonPropertyName("action")]
    public string Action => "cancel";
}

/// <summary>
/// Request to reschedule an existing appointment.
/// </summary>
public sealed record RescheduleAppointmentRequest
{
    /// <summary>The booking ID to reschedule (e.g. "APT-MN7O3825-TMVP").</summary>
    [JsonPropertyName("bookingId")]
    public required string BookingId { get; init; }

    /// <summary>Email address associated with the booking.</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>New date in YYYY-MM-DD format.</summary>
    [JsonPropertyName("newDate")]
    public required string NewDate { get; init; }

    /// <summary>New start time in HH:mm 24-hour format.</summary>
    [JsonPropertyName("newTime")]
    public required string NewTime { get; init; }

    /// <summary>New end time in HH:mm 24-hour format.</summary>
    [JsonPropertyName("newEndTime")]
    public required string NewEndTime { get; init; }

    /// <summary>Workflow action (always "reschedule" for this request type).</summary>
    [JsonPropertyName("action")]
    public string Action => "reschedule";
}
