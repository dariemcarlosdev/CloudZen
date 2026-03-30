using CloudZen.Features.Landing.Models;
using CloudZen.Features.Landing.Services;
using CloudZen.Features.Projects.Services;
using CloudZen.Features.Projects.Models;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Landing.Components;

/// <summary>
/// Code-behind for CaseStudies.razor — loads featured projects and delegates
/// text transformation to ICaseStudyService.
/// </summary>
public partial class CaseStudies
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private ICaseStudyService CaseStudyService { get; set; } = default!;

    private List<ProjectInfo> _caseStudyProjects = new();

    protected override void OnInitialized()
    {
        _caseStudyProjects = ProjectService
            .GetProjectsByCategory(ProjectCategory.AiAutomation)
            .Take(3)
            .ToList();
    }
}
