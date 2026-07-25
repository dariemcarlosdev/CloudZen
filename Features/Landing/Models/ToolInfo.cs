namespace CloudZen.Features.Landing.Models;

/// <summary>
/// Represents a single tool/feature displayed in the Tools Overview section.
/// </summary>
/// <param name="IconMarkup">The HTML markup for the tool icon (e.g. a Bootstrap Icon).</param>
/// <param name="Title">The tool's display title.</param>
/// <param name="Description">A brief description of what the tool does.</param>
public record ToolInfo(string IconMarkup, string Title, string Description);
