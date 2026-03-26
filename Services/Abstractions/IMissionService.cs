using CloudZen.Models;

namespace CloudZen.Services.Abstractions;

/// <summary>
/// Interface for retrieving CloudZen's mission data and company standards/values.
/// </summary>
public interface IMissionService
{
    List<string> GetMissionPoints();
    List<StandardInfo> GetStandards();
}
