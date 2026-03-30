using CloudZen.Features.Projects.Models;

namespace CloudZen.Features.Landing.Services;

/// <summary>
/// Interface for case study text-transformation and display helpers.
/// Converts technical project data into business-friendly presentation text.
/// </summary>
public interface ICaseStudyService
{
    string GetProjectCategory(string projectType);
    string GetProjectCategory(ProjectCategory category);
    string GetShortTitle(string title);
    string GetCustomerFriendlyDescription(string description);
    string GetSimplifiedResult(string result);
}
