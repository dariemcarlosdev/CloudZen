namespace CloudZen.Features.Landing.Models;

/// <summary>
/// Represents a single feature highlight with alternating text/image layout.
/// </summary>
/// <param name="Subtitle">Optional small text above the title.</param>
/// <param name="TitlePrefix">The regular-weight portion of the title.</param>
/// <param name="TitleBold">The bold/italic keyword in the title.</param>
/// <param name="TitleSuffix">The text after the bold keyword.</param>
/// <param name="Description">A brief description of the feature.</param>
/// <param name="ImagePath">Path to the illustration image.</param>
public record FeatureHighlight(
    string? Subtitle,
    string TitlePrefix,
    string TitleBold,
    string TitleSuffix,
    string Description,
    string ImagePath);
