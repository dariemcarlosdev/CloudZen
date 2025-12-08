namespace CloudZen.Models;

/// <summary>
/// Represents a project showcased in the portfolio.
/// </summary>
public class ProjectInfo
{
    /// <summary>
    /// The name of the project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the project (e.g., "Completed", "In Progress", "Planning").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the project and its objectives.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Array of technologies and tools used in the project.
    /// </summary>
    public string[] TechStack { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Project completion percentage (0-100).
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// List of measurable outcomes and achievements from the project.
    /// </summary>
    public List<string> Results { get; set; } = new();

    /// <summary>
    /// List of participants/contributors to the project.
    /// </summary>
    public IEnumerable<ProjectParticipant> Participants { get; set; } = Enumerable.Empty<ProjectParticipant>();

    /// <summary>
    /// Your role in the project (e.g., "Principal Consultant / Solution Architect").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Main challenges faced during the project execution.
    /// </summary>
    public List<string> Challenges { get; set; } = new();

    /// <summary>
    /// GitHub repository URL for the project (optional).
    /// </summary>
    public string? GithubUrl { get; set; }

    /// <summary>
    /// Type of project: "Side Project", "Client Work", or "Customer: {Name}".
    /// </summary>
    public string ProjectType { get; set; } = string.Empty;
}
