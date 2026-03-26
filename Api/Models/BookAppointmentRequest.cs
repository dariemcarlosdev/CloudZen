using System.Text.Json.Serialization;

namespace CloudZen.Api.Models;

/// <summary>
/// Request model for the BookAppointment function.
/// Matches the JSON contract expected by the n8n appointment webhook.
/// </summary>
public class BookAppointmentRequest
{
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

    /// <summary>Workflow action to perform. Defaults to <c>"book"</c>.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "book";

    /// <summary>Reason for the appointment, displayed in the Google Calendar event.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "CloudZen Virtual Meeting";
}
