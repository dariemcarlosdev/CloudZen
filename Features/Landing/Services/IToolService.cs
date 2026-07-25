using CloudZen.Features.Landing.Models;

namespace CloudZen.Features.Landing.Services;

/// <summary>
/// Interface for retrieving tool/feature items for the Tools Overview section.
/// </summary>
public interface IToolService
{
    List<ToolInfo> GetAllTools();
}
