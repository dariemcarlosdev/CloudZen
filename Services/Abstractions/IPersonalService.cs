using CloudZen.Models;

namespace CloudZen.Services.Abstractions;

/// <summary>
/// Interface for retrieving professional service offerings.
/// </summary>
public interface IPersonalService
{
    List<ServiceInfo> GetAllServices();
}
