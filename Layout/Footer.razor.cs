using Microsoft.AspNetCore.Components;

namespace CloudZen.Layout;

public sealed partial class Footer : ComponentBase
{
    private static int CurrentYear => DateTime.Now.Year;
}
