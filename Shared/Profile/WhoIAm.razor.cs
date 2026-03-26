using CloudZen.Models;
using CloudZen.Services;
using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudZen.Shared.Profile;

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

    protected override void OnInitialized()
    {
        Projects = ProjectService.GetAllProjects();
        FilteredProjects = Projects;
    }

    /// <summary>
    /// Handles filter changes from the ProjectFilter component.
    /// </summary>
    private void HandleFilterChange((string Status, string ProjectType) filters)
    {
        FilteredProjects = Projects
            .Where(p => string.IsNullOrEmpty(filters.Status) || p.Status == filters.Status)
            .Where(p => string.IsNullOrEmpty(filters.ProjectType) ||
                        (filters.ProjectType == "Customer"
                            ? p.ProjectType.StartsWith("Customer:")
                            : p.ProjectType == filters.ProjectType))
            .ToList();
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
