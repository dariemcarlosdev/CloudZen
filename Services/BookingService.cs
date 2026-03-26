using System.Globalization;
using CloudZen.Services.Abstractions;

namespace CloudZen.Services;

/// <summary>
/// Provides calendar logic, date availability checks, and formatting for the booking flow.
/// </summary>
public class BookingService : IBookingService
{
    public string[] AvailableTimeSlots { get; } =
    [
        "10:00 AM", "10:30 AM",
        "12:00 PM", "12:30 PM",
        "01:00 PM",
        "02:30 PM",
        "03:00 PM",
        "05:00 PM"
    ];

    public int?[] BuildCalendarCells(DateTime displayMonth)
    {
        var firstDay = new DateTime(displayMonth.Year, displayMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(displayMonth.Year, displayMonth.Month);

        // Monday = 0 offset
        int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;

        var cells = new int?[startOffset + daysInMonth];
        for (int i = 0; i < startOffset; i++)
            cells[i] = null;
        for (int d = 1; d <= daysInMonth; d++)
            cells[startOffset + d - 1] = d;

        return cells;
    }

    public bool IsDateAvailable(DateTime date)
    {
        return date >= DateTime.Today
            && date.DayOfWeek != DayOfWeek.Saturday
            && date.DayOfWeek != DayOfWeek.Sunday;
    }

    public bool IsPreviousMonthDisabled(DateTime displayMonth)
    {
        return displayMonth.Year == DateTime.Today.Year && displayMonth.Month == DateTime.Today.Month;
    }

    public string FormatSlotRange(string? selectedTime)
    {
        if (selectedTime is null) return string.Empty;

        if (DateTime.TryParseExact(selectedTime, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            var end = start.AddMinutes(30);
            return $"{start:hh:mm tt} - {end:hh:mm tt}";
        }

        return selectedTime;
    }

    public string FormatTimeZoneOption(TimeZoneInfo tz)
    {
        var utcOffset = tz.BaseUtcOffset;
        var sign = utcOffset >= TimeSpan.Zero ? "+" : "-";
        return $"GMT{sign}{Math.Abs(utcOffset.Hours):00}:{Math.Abs(utcOffset.Minutes):00} {tz.Id} ({tz.StandardName})";
    }

    public string GetLocalTimeZoneLabel()
    {
        return FormatTimeZoneOption(TimeZoneInfo.Local);
    }
}
