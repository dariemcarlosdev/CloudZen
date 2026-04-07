using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudZen.Features.Profile.Components;

public sealed partial class SDLCProcess : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./js/timeline-observer.js");
            await _module.InvokeVoidAsync("initTimelineObserver");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("destroyTimelineObserver");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already closed — safe to ignore
            }
        }
    }
}
