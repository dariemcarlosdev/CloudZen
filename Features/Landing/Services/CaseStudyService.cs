
namespace CloudZen.Features.Landing.Services;

/// <summary>
/// Converts technical project data into business-friendly presentation text
/// for the case studies section.
/// </summary>
public class CaseStudyService : ICaseStudyService
{
    /// <summary>
    /// Determines the display category badge for a project based on its type.
    /// </summary>
    public string GetProjectCategory(string projectType)
    {
        if (projectType.Contains("Customer"))
        {
            return "Customer Success";
        }
        return "Innovation Project";
    }

    /// <summary>
    /// Converts long project titles into shorter, more display-friendly versions.
    /// </summary>
    public string GetShortTitle(string title)
    {
        if (title.Contains("WPBT"))
            return "Assessment Platform Modernization";
        if (title.Contains("ETL Optimization"))
            return "Data Pipeline Optimization";
        if (title.Contains("VPKFILEPROCESSOR"))
            return "File Processing Automation";
        if (title.Contains("Smart Menu"))
            return "AI Menu Optimization";

        return title.Length > 50 ? title.Substring(0, 47) + "..." : title;
    }

    /// <summary>
    /// Translates technical descriptions into business-friendly language.
    /// </summary>
    public string GetCustomerFriendlyDescription(string description)
    {
        var simplified = description
            .Replace("ASP.NET Web Forms to modular ASP.NET Core architecture", "outdated systems to modern technology")
            .Replace("SSIS ETL pipeline", "data processing pipeline")
            .Replace("ABAP-driven delta extraction", "smart data extraction")
            .Replace("cloud-native solution", "modern online solution")
            .Replace("Blazor Server interface", "user-friendly web interface")
            .Replace("Azure Event Grid", "automated notifications");

        return simplified.Length > 150 ? simplified.Substring(0, 147) + "..." : simplified;
    }

    /// <summary>
    /// Simplifies technical result statements for non-technical audiences.
    /// </summary>
    public string GetSimplifiedResult(string result)
    {
        var simplified = result
            .Replace("turnaround times by roughly", "delivery speed by")
            .Replace("Runtime Reduction through Delta Processing", "faster processing")
            .Replace("Scales-Out efficiently with large datasets", "Handles growing data smoothly")
            .Replace("CI/CD pipelines", "automated deployments")
            .Replace("Azure Event Grid", "automated notifications");

        return simplified.Length > 80 ? simplified.Substring(0, 77) + "..." : simplified;
    }
}
