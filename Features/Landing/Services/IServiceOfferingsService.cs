using CloudZen.Features.Landing.Models;

namespace CloudZen.Features.Landing.Services;

/// <summary>
/// Interface for retrieving professional service offerings.
/// </summary>
public interface IServiceOfferingsService
{
    List<ServiceInfo> GetAllServices();
}
