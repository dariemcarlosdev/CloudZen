using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing.Booking;

/// <summary>
/// Code-behind for BookingTimeSlots.razor — time slot selection panel.
/// </summary>
public partial class BookingTimeSlots
{
    [Parameter, EditorRequired] public string[] TimeSlots { get; set; } = [];
    [Parameter] public string? SelectedTime { get; set; }
    [Parameter] public EventCallback<string> OnTimeSelected { get; set; }
    [Parameter] public EventCallback OnConfirmed { get; set; }

    private static string GetTimeSlotCss(bool isSelected)
    {
        const string baseClass = "px-3 py-2 rounded-lg text-sm font-semibold border transition text-center";

        return isSelected
            ? $"{baseClass} bg-teal-cyan-aqua-500 text-white border-teal-cyan-aqua-500"
            : $"{baseClass} border-teal-cyan-aqua-300 text-teal-cyan-aqua-500 hover:bg-teal-cyan-aqua-50";
    }
}
