using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing.Booking;

/// <summary>
/// Code-behind for BookingConfirmation.razor — Step 3 success confirmation.
/// </summary>
public partial class BookingConfirmation
{
    [Parameter, EditorRequired] public string FullName { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Email { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string TimeSlotRange { get; set; } = string.Empty;
    [Parameter, EditorRequired] public DateTime SelectedDate { get; set; }
    [Parameter] public EventCallback OnReset { get; set; }
}
