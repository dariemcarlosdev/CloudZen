using System.Globalization;

namespace CloudZen.Services.Abstractions;

/// <summary>
/// Service for booking calendar logic, date availability, and formatting.
/// </summary>
public interface IBookingService
{
    /// <summary>Available 30-minute time slots offered each day.</summary>
    string[] AvailableTimeSlots { get; }

    /// <summary>Builds calendar grid cells for a given month (null = empty leading cell).</summary>
    int?[] BuildCalendarCells(DateTime displayMonth);

    /// <summary>Returns true if the given date is bookable (weekday, today or future).</summary>
    bool IsDateAvailable(DateTime date);

    /// <summary>Returns true if navigating to the previous month should be disabled.</summary>
    bool IsPreviousMonthDisabled(DateTime displayMonth);

    /// <summary>Formats a time slot as a 30-min range, e.g. "12:30 PM - 01:00 PM".</summary>
    string FormatSlotRange(string? selectedTime);

    /// <summary>Formats a time zone for display, e.g. "GMT+05:30 India Standard Time (IST)".</summary>
    string FormatTimeZoneOption(TimeZoneInfo tz);

    /// <summary>Returns the display label for the local time zone.</summary>
    string GetLocalTimeZoneLabel();
}
