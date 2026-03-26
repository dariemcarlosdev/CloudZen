using CloudZen.Models;
using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing;

/// <summary>
/// Code-behind for Services.razor — loads service offerings split into featured and remaining.
/// </summary>
public partial class Services
{
    [Inject] private IPersonalService ProfessionalService { get; set; } = default!;

    private List<ServiceInfo> _featured = new();
    private List<ServiceInfo> _remaining = new();

    protected override void OnInitialized()
    {
        var all = ProfessionalService.GetAllServices();
        _featured = all.Take(3).ToList();
        _remaining = all.Skip(3).ToList();
    }
}
