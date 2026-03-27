# Blazor Component Parameter Mismatch Error

## Error Message

```
Unhandled exception rendering component: Object of type 'CloudZen.Features.Booking.Components.BookingCalendar' does not have a property matching the name 'OnMonthChanged'.
System.InvalidOperationException: Object of type 'CloudZen.Features.Booking.Components.BookingCalendar' does not have a property matching the name 'OnMonthChanged'.
   at Microsoft.AspNetCore.Components.Reflection.ComponentProperties.ThrowForUnknownIncomingParameterName(Type targetType, String parameterName)
   at Microsoft.AspNetCore.Components.Reflection.ComponentProperties.SetProperties(ParameterView& parameters, Object target)
   at Microsoft.AspNetCore.Components.ParameterView.SetParameterProperties(Object target)
   at Microsoft.AspNetCore.Components.ComponentBase.SetParametersAsync(ParameterView parameters)
   at Microsoft.AspNetCore.Components.Rendering.ComponentState.SupplyCombinedParameters(ParameterView directAndCascadingParameters)
```

## Cause

A parent component is passing a parameter to a child component that doesn't exist on the child component's `[Parameter]` properties.

In this specific case, `ManageAppointmentReschedule.razor` was passing:
- `OnMonthChanged` — **incorrect**
- `IsPreviousMonthDisabled` — **not a parameter** (handled internally)
- `IsDateAvailable` — **not a parameter** (handled internally)

## Resolution

### 1. Identify the correct parameter name

Check the child component's code-behind (`.razor.cs`) for the actual `[Parameter]` properties:

```csharp
// BookingCalendar.razor.cs
[Parameter, EditorRequired] public DateTime DisplayMonth { get; set; }
[Parameter] public DateTime? SelectedDate { get; set; }
[Parameter] public string TimeZoneLabel { get; set; } = string.Empty;
[Parameter] public EventCallback<DateTime> OnDateSelected { get; set; }
[Parameter] public EventCallback<DateTime> OnDisplayMonthChanged { get; set; }  // ✅ Correct name
[Parameter] public EventCallback<(string Id, string Label)> OnTimeZoneChanged { get; set; }
```

### 2. Update the parent component

**Before (incorrect):**
```razor
<BookingCalendar DisplayMonth="@displayMonth"
                 SelectedDate="@selectedDate"
                 OnDateSelected="@SelectDate"
                 OnMonthChanged="@SetDisplayMonth"
                 IsPreviousMonthDisabled="@BookingService.IsPreviousMonthDisabled(displayMonth)"
                 IsDateAvailable="@BookingService.IsDateAvailable" />
```

**After (correct):**
```razor
<BookingCalendar DisplayMonth="@displayMonth"
                 SelectedDate="@selectedDate"
                 OnDateSelected="@SelectDate"
                 OnDisplayMonthChanged="@SetDisplayMonth" />
```

### 3. Apply the fix to the running application

After making code changes during a debug session, you must apply them:

| Method | How |
|--------|-----|
| **Hot Reload** | `Ctrl+Shift+Enter` or click the 🔥 Hot Reload button |
| **Restart Debug** | `Shift+F5` to stop, then `F5` to start |

> **Note:** Blazor WebAssembly requires Hot Reload or a full restart for Razor component changes to take effect.

## Prevention

1. **Use `EditorRequired`** on mandatory parameters to get compile-time warnings.
2. **Consistent naming** — Follow the pattern `On<Event>` for `EventCallback` parameters.
3. **Keep internal logic internal** — Don't expose parameters for logic the component handles via injected services (e.g., `IsPreviousMonthDisabled`, `IsDateAvailable` are handled by `IBookingService` inside `BookingCalendar`).

## Related Files

- `Features/Booking/Components/BookingCalendar.razor`
- `Features/Booking/Components/BookingCalendar.razor.cs`
- `Features/Booking/Components/ManageAppointmentReschedule.razor`
