using CloudZen.Models;

namespace CloudZen.Services.Abstractions;

/// <summary>
/// Interface for retrieving feature highlights for the Features Showcase section.
/// </summary>
public interface IFeatureHighlightService
{
    List<FeatureHighlight> GetAllFeatures();
}
