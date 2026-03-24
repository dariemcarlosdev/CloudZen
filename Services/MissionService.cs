using CloudZen.Models;

namespace CloudZen.Services;

/// <summary>
/// Provides CloudZen's mission data and company standards/values.
/// </summary>
public class MissionService
{
    /// <summary>
    /// Returns the list of capabilities CloudZen helps businesses with.
    /// Displayed as a checklist in the mission section.
    /// </summary>
    public List<string> GetMissionPoints() => new()
    {
        "System Modernization",
        "Smart Automation",
        "Cloud Migration",
        "Data-Driven Insights",
        "And So Much More!"
    };

    /// <summary>
    /// Returns CloudZen's core standards and values.
    /// Displayed as a 3-column icon grid.
    /// </summary>
    public List<StandardInfo> GetStandards() => new()
    {
        new StandardInfo(
            IconClass: "bi-arrow-repeat",
            Title: "Staying Relevant",
            Description: "We keep your technology current so you can serve your customers at a higher level."
        ),
        new StandardInfo(
            IconClass: "bi-graph-up-arrow",
            Title: "Maximum Growth",
            Description: "Our goal is to give you the tools and resources to maximize your business growth."
        ),
        new StandardInfo(
            IconClass: "bi-heart",
            Title: "Positive Impact",
            Description: "We want to help you amplify the positive impact you have in your community."
        ),
        new StandardInfo(
            IconClass: "bi-grid-3x3-gap",
            Title: "Cross-Functional",
            Description: "Our solutions give you the ability to perform at your best across all platforms."
        ),
        new StandardInfo(
            IconClass: "bi-people",
            Title: "Multidisciplinary Team",
            Description: "We bring in a highly diverse team in skills and culture to serve you better."
        ),
        new StandardInfo(
            IconClass: "bi-cpu",
            Title: "Cutting-Edge Technology",
            Description: "We strive to bring you the best tools the market has to offer."
        )
    };
}
