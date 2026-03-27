namespace CloudZen.Features.Landing.Models;

/// <summary>
/// Represents a single standard/value displayed in the "Our Standards" grid.
/// </summary>
/// <param name="IconClass">Bootstrap Icon class (e.g. "bi-lightning-charge").</param>
/// <param name="Title">The standard's display title.</param>
/// <param name="Description">A brief description of the standard.</param>
public record StandardInfo(string IconClass, string Title, string Description);
