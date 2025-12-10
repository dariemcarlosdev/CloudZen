namespace CloudZen.Models;

/// <summary>
/// Represents a professional service offering.
/// </summary>
/// <param name="Icon">The emoji or icon representing the service.</param>
/// <param name="Title">The service title.</param>
/// <param name="Description">A brief description of the service offering.</param>
public record ServiceInfo(string Icon, string Title, string Description);
