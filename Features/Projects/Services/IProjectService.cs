using CloudZen.Features.Projects.Models;

namespace CloudZen.Features.Projects.Services;

/// <summary>
/// Interface for retrieving project portfolio data.
/// </summary>
public interface IProjectService
{
    List<ProjectInfo> GetAllProjects();
    List<ProjectInfo> GetProjectsByStatus(string status);
    List<ProjectInfo> GetProjectsByType(string projectType);
}
