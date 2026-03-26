using CloudZen.Models;
using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing;

/// <summary>
/// Code-behind for FeaturesShowcase.razor — loads feature highlights from service.
/// </summary>
public partial class FeaturesShowcase
{
    [Inject] private IFeatureHighlightService FeatureHighlightService { get; set; } = default!;

    private List<FeatureHighlight> _features = new();

    protected override void OnInitialized()
    {
        _features = FeatureHighlightService.GetAllFeatures();
    }
}
