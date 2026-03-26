using CloudZen.Models;

namespace CloudZen.Services.Abstractions;

/// <summary>
/// Interface for retrieving tool/feature items for the Tools Overview section.
/// </summary>
public interface IToolService
{
    List<ToolInfo> GetAllTools();
}
