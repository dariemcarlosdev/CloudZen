using CloudZen.Models;
using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing;

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
