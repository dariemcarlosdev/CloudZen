using CloudZen.Features.Booking.Models;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Booking.Components;

/// <summary>
/// Code-behind for BookingDetailsForm.razor — Step 2 form for entering booking details.
/// </summary>
public partial class BookingDetailsForm
{
    [Parameter, EditorRequired] public BookingFormModel FormModel { get; set; } = default!;
    [Parameter] public bool IsSubmitting { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }

    /// <summary>Indicates the error is a scheduling conflict (slot already booked).</summary>
    [Parameter] public bool IsSlotTaken { get; set; }

    [Parameter] public EventCallback OnValidSubmit { get; set; }

    /// <summary>Callback invoked when the user clicks "Choose a different time" after a slot-taken error.</summary>
    [Parameter] public EventCallback OnChooseDifferentTime { get; set; }
}
