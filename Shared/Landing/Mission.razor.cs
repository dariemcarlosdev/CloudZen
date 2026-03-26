using CloudZen.Models;
using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing;

/// <summary>
/// Code-behind for Mission.razor — loads mission points and standards data.
/// </summary>
public partial class Mission
{
    [Inject] private IMissionService MissionService { get; set; } = default!;

    private List<string> _missionPoints = new();
    private List<StandardInfo> _standards = new();

    protected override void OnInitialized()
    {
        _missionPoints = MissionService.GetMissionPoints();
        _standards = MissionService.GetStandards();
    }
}
