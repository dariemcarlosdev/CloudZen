using CloudZen.Features.Landing.Models;
using CloudZen.Features.Landing.Services;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Landing.Components;

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
