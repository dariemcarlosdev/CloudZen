using CloudZen.Features.Booking.Models;
using CloudZen.Features.Booking.Services;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Booking.Components;

/// <summary>
/// Code-behind for ManageAppointmentReschedule.razor — handles appointment rescheduling flow.
/// Manages a two-step wizard: (1) enter booking details, (2) select new date/time.
/// </summary>
/// <remarks>
/// <para><b>Single Responsibility:</b> Manages only the rescheduling workflow state and user interactions.</para>
/// <para><b>Dependency Inversion:</b> Depends on <see cref="IAppointmentService"/> and <see cref="IBookingService"/> abstractions.</para>
/// <para><b>Open/Closed:</b> New steps can be added by extending the <see cref="Step"/> enum without modifying existing logic.</para>
/// </remarks>
public partial class ManageAppointmentReschedule
{
    // ── Dependencies ──────────────────────────────────────────────────────

    /// <summary>
    /// Service for appointment operations (cancel, reschedule, book).
    /// Injected via DI; depends on abstraction per Dependency Inversion Principle.
    /// </summary>
    [Inject] private IAppointmentService AppointmentService { get; set; } = default!;

    /// <summary>
    /// Service for calendar logic, date availability, time formatting, and time zone handling.
    /// </summary>
    [Inject] private IBookingService BookingService { get; set; } = default!;

    // ── State: Wizard Flow ────────────────────────────────────────────────

    /// <summary>Defines the steps in the rescheduling wizard flow.</summary>
    private enum Step { EnterDetails, SelectDateTime }

    /// <summary>The currently active wizard step.</summary>
    private Step currentStep = Step.EnterDetails;

    // ── State: Form Data ──────────────────────────────────────────────────

    /// <summary>Form model bound to the reschedule form inputs (booking ID and email).</summary>
    private RescheduleFormModel rescheduleForm = new();

    /// <summary>First day of the month currently shown in the calendar grid.</summary>
    private DateTime displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    /// <summary>The date the user selected in the calendar.</summary>
    private DateTime? selectedDate;

    /// <summary>The 12-hour time slot the user selected (e.g. <c>"01:00 PM"</c>).</summary>
    private string? selectedTime;

    /// <summary>Display label for the selected time zone (e.g. <c>"GMT-05:00 America/New_York (EST)"</c>).</summary>
    private string timeZoneLabel = string.Empty;

    // ── State: UI Feedback ────────────────────────────────────────────────

    /// <summary>Indicates whether a reschedule request is currently in flight.</summary>
    private bool isSubmitting;

    /// <summary>Indicates whether the reschedule was successful (shows confirmation UI).</summary>
    private bool isConfirmed;

    /// <summary>User-facing error message displayed when rescheduling fails.</summary>
    private string? errorMessage;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the default time zone label on first render.
    /// </summary>
    protected override void OnInitialized()
    {
        timeZoneLabel = BookingService.GetLocalTimeZoneLabel();
    }

    // ── Step Navigation ───────────────────────────────────────────────────

    /// <summary>
    /// Advances from Step 1 (enter details) to Step 2 (select date/time).
    /// Called when the form in Step 1 passes validation.
    /// </summary>
    private void GoToSelectDateTime()
    {
        errorMessage = null;
        currentStep = Step.SelectDateTime;
    }

    /// <summary>
    /// Returns from Step 2 (select date/time) back to Step 1 (enter details).
    /// </summary>
    private void GoBackToDetails()
    {
        errorMessage = null;
        currentStep = Step.EnterDetails;
    }

    // ── Date/Time Selection Handlers ──────────────────────────────────────

    /// <summary>
    /// Handles a date selection from <see cref="BookingCalendar"/>.
    /// Resets <see cref="selectedTime"/> since a new date invalidates any prior time pick.
    /// </summary>
    /// <param name="date">The newly selected calendar date.</param>
    private void SelectDate(DateTime date)
    {
        selectedDate = date;
        selectedTime = null;
    }

    /// <summary>
    /// Stores the time slot selected by the user in <see cref="BookingTimeSlots"/>.
    /// </summary>
    /// <param name="time">The selected time slot (e.g. <c>"10:00 AM"</c>).</param>
    private void SelectTime(string time) => selectedTime = time;

    /// <summary>
    /// Updates the calendar grid to display a different month.
    /// </summary>
    /// <param name="month">The first day of the month to display.</param>
    private void SetDisplayMonth(DateTime month) => displayMonth = month;

    /// <summary>
    /// Handles a time zone change from <see cref="BookingTimeZonePicker"/>.
    /// Updates the display label shown in the sidebar.
    /// </summary>
    /// <param name="tz">Tuple of the selected time zone ID and its formatted display label.</param>
    private void HandleTimeZoneChanged((string Id, string Label) tz)
    {
        timeZoneLabel = tz.Label;
    }

    // ── Formatting Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Formats a time slot into a display range (e.g. <c>"10:00 AM - 10:30 AM"</c>).
    /// Delegates to <see cref="IBookingService.FormatSlotRange"/>.
    /// </summary>
    /// <param name="time">The start time of the slot.</param>
    /// <returns>Formatted time range string.</returns>
    private string FormatSlotRange(string? time)
    {
        return BookingService.FormatSlotRange(time);
    }

    // ── Form Submission ───────────────────────────────────────────────────

    /// <summary>
    /// Handles the final form submission for appointment rescheduling.
    /// Validates state, calls the appointment service, and updates UI accordingly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleReschedule()
    {
        if (!selectedDate.HasValue || string.IsNullOrEmpty(selectedTime))
            return;

        isSubmitting = true;
        errorMessage = null;

        try
        {
            var request = new RescheduleAppointmentRequest
            {
                BookingId = rescheduleForm.BookingId!,
                Email = rescheduleForm.Email!,
                NewDate = selectedDate.Value.ToString("yyyy-MM-dd"),
                NewTime = BookingService.FormatTimeTo24Hour(selectedTime),
                NewEndTime = BookingService.FormatEndTimeTo24Hour(selectedTime)
            };

            var result = await AppointmentService.RescheduleAsync(request);

            if (result.Success)
            {
                isConfirmed = true;
            }
            else
            {
                errorMessage = result.Error ?? "We couldn't reschedule your appointment. Please try again.";
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
        rescheduleForm = new RescheduleFormModel();
        selectedDate = null;
        selectedTime = null;
        displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        currentStep = Step.EnterDetails;
        isConfirmed = false;
        errorMessage = null;
    }
}
