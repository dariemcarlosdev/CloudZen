using CloudZen.Features.Booking.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudZen.Features.Landing.Components;

/// <summary>
/// Code-behind for CTA.razor — opens a pre-filled Google Calendar event.
/// </summary>
public partial class CTA
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IGoogleCalendarUrlService CalendarUrlService { get; set; } = default!;

    private async Task BookConsultation()
    {
        var url = CalendarUrlService.CreateConsultationUrl();
        await JS.InvokeVoidAsync("open", url, "_blank");
    }
}
