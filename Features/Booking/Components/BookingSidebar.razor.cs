using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Booking.Components;

/// <summary>
/// Code-behind for BookingSidebar.razor — left sidebar with meeting info.
/// </summary>
public partial class BookingSidebar
{
    [Parameter] public DateTime? SelectedDate { get; set; }
    [Parameter] public string? TimeSlotRange { get; set; }
    [Parameter] public string? TimeZoneLabel { get; set; }
    [Parameter] public bool ShowBackButton { get; set; }
    [Parameter] public EventCallback OnBackClicked { get; set; }
}
