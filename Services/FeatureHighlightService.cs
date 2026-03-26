using CloudZen.Models;
using CloudZen.Services.Abstractions;

namespace CloudZen.Services;

/// <summary>
/// Provides the list of feature highlights displayed in the Features Showcase section.
/// </summary>
public class FeatureHighlightService : IFeatureHighlightService
{
    public List<FeatureHighlight> GetAllFeatures() => new()
    {
        new FeatureHighlight(
            Subtitle: "Reclaim Your Calendar",
            TitlePrefix: "Put the Busy Work on ",
            TitleBold: "Autopilot",
            TitleSuffix: "",
            Description: "Stop wasting hours on repetitive tasks. CloudZen sets up smart systems that handle the boring stuff — scheduling, data entry, follow-ups — so you can focus on the work that actually makes you money.",
            ImagePath: "/images/features/autopilot.webp"
        ),
        new FeatureHighlight(
            Subtitle: "Built Around Your Workflow",
            TitlePrefix: "",
            TitleBold: "Custom Systems",
            TitleSuffix: " That Work the Way You Do",
            Description: "Stop trying to fit your business into a box. CloudZen builds tools designed around your specific daily routine and goals, so your technology finally supports you instead of getting in your way.",
            ImagePath: "/images/features/custom-systems.webp"
        ),
        new FeatureHighlight(
            Subtitle: "See What Matters at a Glance",
            TitlePrefix: "Clear, Simple ",
            TitleBold: "Insights",
            TitleSuffix: " From Your Data",
            Description: "Stop digging through messy spreadsheets. We create simple, clear dashboards that show you exactly how your business is performing, so you can make decisions with total confidence.",
            ImagePath: "/images/features/data-insights.webp"
        ),
        new FeatureHighlight(
            Subtitle: "Out With the Old",
            TitlePrefix: "",
            TitleBold: "Modernize",
            TitleSuffix: " Your Systems, Keep Your Momentum",
            Description: "Tired of outdated systems holding you back? We help you transition smoothly to modern solutions that keep your business running while setting you up for the future — no downtime, no disruption.",
            ImagePath: "/images/features/modernize-systems.webp"
        ),
        new FeatureHighlight(
            Subtitle: "Speed Wins Customers",
            TitlePrefix: "",
            TitleBold: "Faster Results",
            TitleSuffix: " Mean Happier Customers",
            Description: "CloudZen helps you clear the bottlenecks so you can respond to clients and deliver your services quicker. When you move faster, your customers stay happier — and keep coming back.",
            ImagePath: "/images/features/faster-result-1.webp"
        )
    };
}
