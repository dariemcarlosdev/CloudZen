using System.Globalization;

namespace CloudZen.Services.Abstractions;

/// <summary>
/// Service for booking calendar logic, date availability, and formatting.
/// </summary>
public interface IBookingService
{
    /// <summary>Available 30-minute time slots offered each day.</summary>
    string[] AvailableTimeSlots { get; }

    /// <summary>Builds calendar grid cells for a given month.</summary>
    /// <param name="displayMonth">The first day of the month to render.</param>
    /// <returns>
    /// An array where <c>null</c> entries represent empty leading cells (before the 1st)
    /// and integer entries represent day numbers.
    /// </returns>
    int?[] BuildCalendarCells(DateTime displayMonth);

    /// <summary>Returns <c>true</c> if the given date is bookable (weekday, today or future).</summary>
    /// <param name="date">The calendar date to check.</param>
    /// <returns><c>true</c> when the date is a weekday on or after today; otherwise <c>false</c>.</returns>
    bool IsDateAvailable(DateTime date);

    /// <summary>Returns <c>true</c> if navigating to the previous month should be disabled.</summary>
    /// <param name="displayMonth">The first day of the currently displayed month.</param>
    /// <returns><c>true</c> when the displayed month is the current calendar month.</returns>
    bool IsPreviousMonthDisabled(DateTime displayMonth);

    /// <summary>Formats a time slot as a 30-min range, e.g. <c>"12:30 PM - 01:00 PM"</c>.</summary>
    /// <param name="selectedTime">A 12-hour slot string (e.g. <c>"12:30 PM"</c>), or <c>null</c>.</param>
    /// <returns>The formatted range, or <see cref="string.Empty"/> when <paramref name="selectedTime"/> is <c>null</c>.</returns>
    string FormatSlotRange(string? selectedTime);

    /// <summary>Formats a time zone for display, e.g. <c>"GMT+05:30 India Standard Time (IST)"</c>.</summary>
    /// <param name="tz">The <see cref="TimeZoneInfo"/> to format.</param>
    /// <returns>A human-readable string with GMT offset, time zone ID, and standard name.</returns>
    string FormatTimeZoneOption(TimeZoneInfo tz);

    /// <summary>Returns the display label for the local time zone.</summary>
    string GetLocalTimeZoneLabel();

    /// <summary>
    /// Converts a 12-hour display slot (e.g. <c>"01:00 PM"</c>) to 24-hour <c>"HH:mm"</c> format
    /// required by the n8n webhook.
    /// </summary>
    /// <param name="displayTime">The 12-hour time string to convert.</param>
    /// <returns>The time in <c>"HH:mm"</c> format, or the original string if parsing fails.</returns>
    string FormatTimeTo24Hour(string displayTime);

    /// <summary>
    /// Returns the 24-hour end time (start + 30 min) for a given 12-hour display slot.
    /// </summary>
    /// <param name="displayTime">The 12-hour time string representing the start of the slot.</param>
    /// <returns>The end time in <c>"HH:mm"</c> format, or the original string if parsing fails.</returns>
    string FormatEndTimeTo24Hour(string displayTime);
}
