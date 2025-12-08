namespace CloudZen.Models;

/// <summary>
/// Represents a participant/contributor in a project.
/// </summary>
public class ProjectParticipant
{
    /// <summary>
    /// The name of the participant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL to the participant's avatar/profile image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
}
