using CloudZen.Models;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing.Booking;

/// <summary>
/// Code-behind for BookingDetailsForm.razor — Step 2 form for entering booking details.
/// </summary>
public partial class BookingDetailsForm
{
    [Parameter, EditorRequired] public BookingFormModel FormModel { get; set; } = default!;
    [Parameter] public bool IsSubmitting { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnValidSubmit { get; set; }
}
