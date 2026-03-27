using System.Text.Json.Serialization;

namespace CloudZen.Features.Booking.Models;

/// <summary>
/// Request payload for the n8n appointment booking webhook.
/// JSON property names use camelCase to match the expected API contract.
/// </summary>
public class BookingAppointmentRequest
{
    /// <summary>
    /// Full name of the person booking the appointment.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the person booking the appointment.
    /// Used by the n8n workflow to send calendar invites and confirmations.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone in E.164 format (e.g. "+15551234567") for Twilio compatibility.
    /// </summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Name of the business or organization the person represents.
    /// </summary>
    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Date in YYYY-MM-DD format.</summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>Start time in HH:mm 24-hour format.</summary>
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    /// <summary>End time in HH:mm 24-hour format (start + 30 min).</summary>
    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// The workflow action to perform. Defaults to <c>"book"</c>.
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "book";

    /// <summary>
    /// Reason for the appointment, displayed in the Google Calendar event.
    /// Defaults to <c>"CloudZen Virtual Meeting"</c>.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "CloudZen Meeting Request";
}
