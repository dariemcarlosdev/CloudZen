using CloudZen.Features.Booking.Models;
using CloudZen.Features.Booking.Services;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Booking.Components;

/// <summary>
/// Code-behind for ManageAppointmentCancel.razor — handles appointment cancellation flow.
/// Manages form state, validation, and communication with the appointment service.
/// </summary>
/// <remarks>
/// <para><b>Single Responsibility:</b> Manages only the cancellation workflow state and user interactions.</para>
/// <para><b>Dependency Inversion:</b> Depends on <see cref="IAppointmentService"/> abstraction, not concrete implementation.</para>
/// </remarks>
public partial class ManageAppointmentCancel
{
    // ── Dependencies ──────────────────────────────────────────────────────

    /// <summary>
    /// Service for appointment operations (cancel, reschedule, book).
    /// Injected via DI; depends on abstraction per Dependency Inversion Principle.
    /// </summary>
    [Inject] private IAppointmentService AppointmentService { get; set; } = default!;

    // ── State ─────────────────────────────────────────────────────────────

    /// <summary>Form model bound to the cancellation form inputs.</summary>
    private CancelFormModel cancelForm = new();

    /// <summary>Indicates whether a cancellation request is currently in flight.</summary>
    private bool isSubmitting;

    /// <summary>Indicates whether the cancellation was successful (shows confirmation UI).</summary>
    private bool isConfirmed;

    /// <summary>User-facing error message displayed when cancellation fails.</summary>
    private string? errorMessage;

    // ── Event Handlers ────────────────────────────────────────────────────

    /// <summary>
    /// Handles the form submission for appointment cancellation.
    /// Validates input, calls the appointment service, and updates UI state accordingly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleCancel()
    {
        isSubmitting = true;
        errorMessage = null;

        try
        {
            var request = new CancelAppointmentRequest
            {
                BookingId = cancelForm.BookingId!,
                Email = cancelForm.Email!
            };

            var result = await AppointmentService.CancelAsync(request);

            if (result.Success)
            {
                isConfirmed = true;
            }
            else
            {
                errorMessage = result.Error ?? "We couldn't cancel your appointment. Please try again.";
            }
        }
        catch
        {
            errorMessage = "Something went wrong. Please try again later.";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    /// <summary>
    /// Resets the component to its initial state, allowing the user to manage another appointment.
    /// </summary>
    private void Reset()
    {
        cancelForm = new CancelFormModel();
        isConfirmed = false;
        errorMessage = null;
    }
}
