using CloudZen.Models;
using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing;

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
        var allProjects = ProjectService.GetAllProjects();

        _caseStudyProjects = allProjects
            .Where(p => (p.Status == "Completed" || p.Status == "In Progress") &&
                       (p.ProjectType.Contains("Customer") ||
                        p.Name.Contains("FILE PROCESSOR") ||
                        p.Name.Contains("Smart Menu")))
            .Take(3)
            .ToList();
    }
}
