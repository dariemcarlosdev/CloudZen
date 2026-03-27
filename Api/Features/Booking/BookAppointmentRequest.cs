using System.Text.Json.Serialization;

namespace CloudZen.Api.Features.Booking;

/// <summary>
/// Request model for the BookAppointment function.
/// Supports <c>book</c>, <c>cancel</c>, and <c>reschedule</c> actions via the <see cref="Action"/> field.
/// </summary>
/// <remarks>
/// <para>
/// This is the WASM client's JSON contract. The Azure Function transforms it to
/// <see cref="N8nAppointmentPayload"/> before forwarding to n8n.
/// </para>
/// <para>
/// Required fields vary by action:
/// <list type="bullet">
///   <item><description><b>book</b>: Name, Email, Phone, BusinessName, Date, Time, EndTime</description></item>
///   <item><description><b>cancel</b>: BookingId, Email</description></item>
///   <item><description><b>reschedule</b>: BookingId, Email, NewDate, NewTime, NewEndTime</description></item>
/// </list>
/// </para>
/// </remarks>
public class BookAppointmentRequest
{
    /// <summary>
    /// Workflow action to perform: <c>"book"</c>, <c>"cancel"</c>, or <c>"reschedule"</c>.
    /// Defaults to <c>"book"</c>.
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "book";

    /// <summary>
    /// Unique booking ID (e.g. <c>"APT-MN7O3825-TMVP"</c>).
    /// Required for <c>cancel</c> and <c>reschedule</c> actions.
    /// </summary>
    [JsonPropertyName("bookingId")]
    public string BookingId { get; set; } = string.Empty;

    /// <summary>Full name of the person booking the appointment.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Email address for calendar invites and confirmations.</summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Phone number in E.164 format (e.g. <c>"+15551234567"</c>) for Twilio compatibility.</summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Name of the business or organization.</summary>
    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Appointment date in <c>YYYY-MM-DD</c> format.</summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>Start time in <c>HH:mm</c> 24-hour format.</summary>
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    /// <summary>End time in <c>HH:mm</c> 24-hour format (start + 30 min).</summary>
    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>Reason for the appointment, displayed in the Google Calendar event.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "CloudZen Virtual Meeting";

    /// <summary>
    /// New date for rescheduling in <c>YYYY-MM-DD</c> format.
    /// Required for <c>reschedule</c> action.
    /// </summary>
    [JsonPropertyName("newDate")]
    public string NewDate { get; set; } = string.Empty;

    /// <summary>
    /// New start time for rescheduling in <c>HH:mm</c> 24-hour format.
    /// Required for <c>reschedule</c> action.
    /// </summary>
    [JsonPropertyName("newTime")]
    public string NewTime { get; set; } = string.Empty;

    /// <summary>
    /// New end time for rescheduling in <c>HH:mm</c> 24-hour format.
    /// Required for <c>reschedule</c> action.
    /// </summary>
    [JsonPropertyName("newEndTime")]
    public string NewEndTime { get; set; } = string.Empty;
}
