using CloudZen.Features.Landing.Models;

namespace CloudZen.Features.Landing.Services;

/// <summary>
/// Interface for retrieving feature highlights for the Features Showcase section.
/// </summary>
public interface IFeatureHighlightService
{
    List<FeatureHighlight> GetAllFeatures();
}
