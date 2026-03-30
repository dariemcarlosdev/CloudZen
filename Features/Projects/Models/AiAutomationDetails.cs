namespace CloudZen.Features.Projects.Models;

/// <summary>
/// Holds AI-automation-specific metadata for projects in the <see cref="ProjectCategory.AiAutomation"/> category.
/// Composed into <see cref="ProjectInfo"/> as an optional property.
/// </summary>
public record AiAutomationDetails
{
    /// <summary>Who is this workflow or feature designed for?</summary>
    public required string TargetAudience { get; init; }

    /// <summary>What problem does this workflow or feature solve?</summary>
    public required string ProblemSolved { get; init; }

    /// <summary>Key benefits the customer gains from adopting this solution.</summary>
    public required List<string> CustomerBenefits { get; init; }
}
