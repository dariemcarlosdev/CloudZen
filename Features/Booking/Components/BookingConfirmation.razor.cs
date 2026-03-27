using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Booking.Components;

/// <summary>
/// Code-behind for BookingConfirmation.razor — Step 3 success confirmation.
/// Displays the confirmed booking details including the n8n-assigned booking ID.
/// </summary>
public partial class BookingConfirmation
{
    /// <summary>Full name of the person who booked the appointment.</summary>
    [Parameter, EditorRequired] public string FullName { get; set; } = string.Empty;

    /// <summary>Email address where the calendar invite will be sent.</summary>
    [Parameter, EditorRequired] public string Email { get; set; } = string.Empty;

    /// <summary>Formatted time slot range (e.g. <c>"02:30 PM - 03:00 PM"</c>).</summary>
    [Parameter, EditorRequired] public string TimeSlotRange { get; set; } = string.Empty;

    /// <summary>The confirmed appointment date.</summary>
    [Parameter, EditorRequired] public DateTime SelectedDate { get; set; }

    /// <summary>
    /// The booking confirmation ID returned by the n8n workflow (e.g. <c>"APT-MN7O3825-TMVP"</c>).
    /// Displayed to the user as a reference for their appointment.
    /// </summary>
    [Parameter] public string? BookingId { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "Schedule Another Meeting"
    /// to reset the booking flow back to Step 1.
    /// </summary>
    [Parameter] public EventCallback OnReset { get; set; }
}
