using CloudZen.Features.Landing.Models;
using CloudZen.Features.Landing.Services;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Landing.Components;

/// <summary>
/// Code-behind for ToolsOverview.razor — loads tool items from service.
/// </summary>
public partial class ToolsOverview
{
    [Inject] private IToolService ToolService { get; set; } = default!;

    private List<ToolInfo> _tools = new();

    protected override void OnInitialized()
    {
        _tools = ToolService.GetAllTools();
    }
}
