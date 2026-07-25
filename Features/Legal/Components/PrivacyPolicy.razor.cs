using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudZen.Features.Legal.Components;

public sealed partial class PrivacyPolicy : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initScrollReveal");
        }
    }
}
