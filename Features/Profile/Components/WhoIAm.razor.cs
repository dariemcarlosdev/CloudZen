using CloudZen.Features.Profile.Models;
using CloudZen.Features.Profile.Services;
using CloudZen.Features.Projects.Services;
using CloudZen.Features.Projects.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudZen.Features.Profile.Components;

/// <summary>
/// Code-behind for WhoIAm.razor — orchestrates project data, filtering,
/// resume download, and scroll-to-section JS interop.
/// </summary>
public partial class WhoIAm
{
    [Inject] private ResumeService ResumeService { get; set; } = default!;
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<ProjectInfo> Projects = new();
    private List<ProjectInfo> FilteredProjects = new();

    // ── Pagination State ─────────────────────────────────────────────────
    private const int PageSize = 5;
    private int _currentPage = 1;

    /// <summary>Current page slice of filtered projects.</summary>
    private List<ProjectInfo> PagedProjects => FilteredProjects
        .Skip((_currentPage - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    protected override void OnInitialized()
    {
        Projects = ProjectService.GetAllProjects();
        FilteredProjects = Projects;
    }

    /// <summary>
    /// Handles filter changes from the ProjectFilter component.
    /// Resets to page 1 whenever filters change.
    /// </summary>
    private void HandleFilterChange((string Status, string ProjectType) filters)
    {
        FilteredProjects = Projects
            .Where(p => string.IsNullOrEmpty(filters.Status) || p.Status == filters.Status)
            .Where(p => string.IsNullOrEmpty(filters.ProjectType) || MatchesProjectTypeFilter(p, filters.ProjectType))
            .ToList();

        _currentPage = 1;
    }

    private static bool MatchesProjectTypeFilter(ProjectInfo project, string filterValue) => filterValue switch
    {
        "Customer" => project.Category == ProjectCategory.CustomerWork,
        "AI Automation" => project.Category == ProjectCategory.AiAutomation,
        "Side Project" => project.Category == ProjectCategory.SideProject,
        _ => project.ProjectType == filterValue
    };

    /// <summary>
    /// Handles page navigation from the Pagination component.
    /// Scrolls to the projects section for smooth UX.
    /// </summary>
    private async Task HandlePageChanged(int page)
    {
        _currentPage = page;
        await JS.InvokeVoidAsync("scrollToElementById", "highlighted-projects");
    }

    /// <summary>
    /// Downloads the resume using ResumeService and JS interop.
    /// </summary>
    private async Task DownloadResume()
    {
        var resumeBytes = await ResumeService.DownloadResumeAsync();
        var uri = new Uri(ResumeService.ResumeBlobUrl);
        var fileName = System.IO.Path.GetFileName(uri.LocalPath);
        await JS.InvokeVoidAsync("saveAsFile", fileName, resumeBytes);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var uri = new Uri(NavigationManager.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var scrollTarget = query["scroll"];
            if (scrollTarget == "highlighted-projects")
            {
                await JS.InvokeVoidAsync("scrollToElementById", "highlighted-projects");
            }
        }
    }
}
