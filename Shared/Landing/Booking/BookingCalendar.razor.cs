using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing.Booking;

/// <summary>
/// Code-behind for BookingCalendar.razor — calendar grid with month navigation.
/// </summary>
public partial class BookingCalendar
{
    [Parameter, EditorRequired] public DateTime DisplayMonth { get; set; }
    [Parameter] public DateTime? SelectedDate { get; set; }
    [Parameter] public string TimeZoneLabel { get; set; } = string.Empty;
    [Parameter] public EventCallback<DateTime> OnDateSelected { get; set; }
    [Parameter] public EventCallback<DateTime> OnDisplayMonthChanged { get; set; }
    [Parameter] public EventCallback<(string Id, string Label)> OnTimeZoneChanged { get; set; }

    [Inject] private IBookingService BookingService { get; set; } = default!;

    private int?[] calendarCells => BookingService.BuildCalendarCells(DisplayMonth);

    private void PreviousMonth()
    {
        if (!BookingService.IsPreviousMonthDisabled(DisplayMonth))
            OnDisplayMonthChanged.InvokeAsync(DisplayMonth.AddMonths(-1));
    }

    private void NextMonth() => OnDisplayMonthChanged.InvokeAsync(DisplayMonth.AddMonths(1));

    private static string GetDayCss(bool isAvailable, bool isSelected, bool isToday)
    {
        const string baseClass = "w-9 h-9 mx-auto rounded-full text-sm flex items-center justify-center transition";

        if (isSelected)
            return $"{baseClass} bg-teal-cyan-aqua-600 text-white font-bold";
        if (!isAvailable)
            return $"{baseClass} text-gray-300 cursor-default";
        if (isToday)
            return $"{baseClass} border-2 border-teal-cyan-aqua-600 text-teal-cyan-aqua-600 font-semibold hover:bg-teal-cyan-aqua-50 cursor-pointer";

        return $"{baseClass} text-teal-cyan-aqua-600 font-semibold hover:bg-teal-cyan-aqua-50 cursor-pointer";
    }
}
