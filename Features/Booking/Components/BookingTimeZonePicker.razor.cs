using CloudZen.Features.Booking.Services;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Booking.Components;

/// <summary>
/// Code-behind for BookingTimeZonePicker.razor — searchable time zone dropdown.
/// </summary>
public partial class BookingTimeZonePicker
{
    [Parameter, EditorRequired] public string TimeZoneLabel { get; set; } = string.Empty;
    [Parameter] public EventCallback<(string Id, string Label)> OnTimeZoneChanged { get; set; }

    [Inject] private IBookingService BookingService { get; set; } = default!;

    private bool isOpen;
    private string searchText = string.Empty;
    private string selectedTimeZoneId = TimeZoneInfo.Local.Id;

    private static readonly TimeZoneInfo[] allTimeZones = TimeZoneInfo.GetSystemTimeZones().ToArray();

    private IEnumerable<TimeZoneInfo> FilteredTimeZones =>
        string.IsNullOrWhiteSpace(searchText)
            ? allTimeZones
            : allTimeZones.Where(tz =>
                BookingService.FormatTimeZoneOption(tz).Contains(searchText, StringComparison.OrdinalIgnoreCase));

    private void ToggleDropdown()
    {
        isOpen = !isOpen;
        if (isOpen)
            searchText = string.Empty;
    }

    private void SelectTimeZone(TimeZoneInfo tz)
    {
        selectedTimeZoneId = tz.Id;
        isOpen = false;
        searchText = string.Empty;
        OnTimeZoneChanged.InvokeAsync((tz.Id, BookingService.FormatTimeZoneOption(tz)));
    }

    private void CloseDropdown()
    {
        isOpen = false;
        searchText = string.Empty;
    }
}
