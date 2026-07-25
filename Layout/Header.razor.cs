using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace CloudZen.Layout;

public sealed partial class Header : ComponentBase, IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private string _activeSection = "#hero";
    private bool _isMobileMenuOpen;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }

    private void SetActive(string section) => _activeSection = section;

    private void ToggleMobileMenu() => _isMobileMenuOpen = !_isMobileMenuOpen;

    private void CloseMobileMenu() => _isMobileMenuOpen = false;

    private void OnMobileNavClick(string section)
    {
        SetActive(section);
        CloseMobileMenu();
    }

    private string GetNavClass(string section) =>
        _activeSection == section
            ? "text-teal-cyan-aqua-500"
            : "text-gray-700 hover:text-teal-cyan-aqua-500";
}
