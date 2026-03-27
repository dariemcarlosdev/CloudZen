using CloudZen.Features.Booking.Models;
using CloudZen.Features.Booking.Services;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Booking.Components;

/// <summary>
/// Code-behind for BookingContact.razor — thin orchestrator holding booking flow state.
/// Composes BookingSidebar, BookingCalendar, BookingTimeSlots, BookingDetailsForm,
/// and BookingConfirmation via parameters and EventCallbacks.
/// </summary>
public partial class BookingContact
{
    /// <summary>Service for sending appointment bookings to the n8n webhook.</summary>
    [Inject] private IAppointmentService AppointmentService { get; set; } = default!;

    /// <summary>Service for calendar logic, date availability, and time formatting.</summary>
    [Inject] private IBookingService BookingService { get; set; } = default!;

    // ── State ────────────────────────────────────────────────────────────

    /// <summary>Defines the three steps in the booking wizard flow.</summary>
    private enum Step { SelectDateTime, EnterDetails, Confirmation }

    /// <summary>The currently active wizard step.</summary>
    private Step currentStep = Step.SelectDateTime;

    /// <summary>First day of the month currently shown in the calendar grid.</summary>
    private DateTime displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    /// <summary>The date the user selected in the calendar (Step 1).</summary>
    private DateTime? selectedDate;

    /// <summary>The 12-hour time slot the user selected (e.g. <c>"01:00 PM"</c>).</summary>
    private string? selectedTime;

    /// <summary>Form model bound to the details form in Step 2.</summary>
    private BookingFormModel bookingForm = new();

    /// <summary>Indicates whether an appointment booking request is in flight.</summary>
    private bool isSubmitting;

    /// <summary>User-facing error message displayed when a booking attempt fails.</summary>
    private string? errorMessage;

    /// <summary>Indicates the last failure was a scheduling conflict (slot already booked).</summary>
    private bool isSlotTaken;

    /// <summary>Display label for the selected time zone (e.g. <c>"GMT-05:00 Eastern Standard Time"</c>).</summary>
    private string timeZoneLabel = string.Empty;

    /// <summary>
    /// The booking confirmation ID returned by the n8n workflow after a successful booking.
    /// Passed to <see cref="Booking.BookingConfirmation"/> in Step 3.
    /// </summary>
    private string? confirmedBookingId;

    /// <summary>
    /// Initializes the default time zone label on first render.
    /// </summary>
    protected override void OnInitialized()
    {
        timeZoneLabel = BookingService.GetLocalTimeZoneLabel();
    }

    // ── Step 1 handlers ──────────────────────────────────────────────────

    /// <summary>
    /// Handles a date selection from <see cref="Booking.BookingCalendar"/>.
    /// Resets <see cref="selectedTime"/> since a new date invalidates any prior time pick.
    /// </summary>
    /// <param name="date">The newly selected calendar date.</param>
    private void SelectDate(DateTime date)
    {
        selectedDate = date;
        selectedTime = null;
    }

    /// <summary>Stores the time slot selected by the user in <see cref="Booking.BookingTimeSlots"/>.</summary>
    private void SelectTime(string time) => selectedTime = time;

    /// <summary>Updates the calendar grid to display a different month.</summary>
    private void SetDisplayMonth(DateTime month) => displayMonth = month;

    /// <summary>
    /// Handles a time zone change from <see cref="Booking.BookingTimeZonePicker"/>.
    /// Updates the display label shown in the sidebar.
    /// </summary>
    /// <param name="tz">Tuple of the selected time zone ID and its formatted display label.</param>
    private void HandleTimeZoneChanged((string Id, string Label) tz)
    {
        timeZoneLabel = tz.Label;
    }

    /// <summary>
    /// Advances from Step 1 (date/time selection) to Step 2 (enter details)
    /// when both <see cref="selectedDate"/> and <see cref="selectedTime"/> are set.
    /// </summary>
    private void ConfirmDateTime()
    {
        if (selectedDate.HasValue && selectedTime is not null)
            currentStep = Step.EnterDetails;
    }

    /// <summary>
    /// Returns from Step 2 to Step 1, clearing any prior error message.
    /// </summary>
    private void GoBackToCalendar()
    {
        errorMessage = null;
        isSlotTaken = false;
        currentStep = Step.SelectDateTime;
    }

    // ── Form submission ──────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="BookingAppointmentRequest"/> from the current form state
    /// and sends it to the n8n webhook via <see cref="IAppointmentService"/>.
    /// On success, transitions to Step 3 (confirmation).
    /// On slot-taken or failure, displays an error and keeps the user on Step 2.
    /// </summary>
    private async Task HandleBookingSubmit()
    {
        isSubmitting = true;
        errorMessage = null;
        isSlotTaken = false;

        try
        {
            var request = new BookingAppointmentRequest
            {
                Name = bookingForm.FullName!,
                Email = bookingForm.Email!,
                Phone = NormalizePhone(bookingForm.Phone!),
                BusinessName = bookingForm.BusinessName!,
                Date = selectedDate!.Value.ToString("yyyy-MM-dd"),
                Time = BookingService.FormatTimeTo24Hour(selectedTime!),
                EndTime = BookingService.FormatEndTimeTo24Hour(selectedTime!),
                Reason = string.IsNullOrWhiteSpace(bookingForm.Reason)
                    ? "CloudZen Virtual Meeting"
                    : bookingForm.Reason
            };

            var result = await AppointmentService.BookAppointmentAsync(request);

            if (result.Success)
            {
                confirmedBookingId = result.BookingId;
                currentStep = Step.Confirmation;
            }
            else
            {
                errorMessage = result.Error ?? "We couldn't schedule your meeting. Please try again.";
                isSlotTaken = result.IsSlotTaken;
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
    /// Resets all booking state back to Step 1 defaults, allowing the user
    /// to schedule another meeting.
    /// </summary>
    private void ResetBooking()
    {
        bookingForm = new BookingFormModel();
        selectedDate = null;
        selectedTime = null;
        displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        currentStep = Step.SelectDateTime;
        errorMessage = null;
        isSlotTaken = false;
        confirmedBookingId = null;
        timeZoneLabel = BookingService.GetLocalTimeZoneLabel();
    }

    /// <summary>
    /// Ensures the phone number is in E.164 format for Twilio compatibility.
    /// Prepends <c>"+1"</c> (US) if no country code is present.
    /// </summary>
    /// <param name="phone">The raw phone number entered by the user.</param>
    /// <returns>
    /// The phone number in E.164 format (e.g. <c>"+15551234567"</c>).
    /// If the input already starts with <c>"+"</c>, digits are preserved as-is.
    /// For 10-digit US numbers, <c>"+1"</c> is prepended automatically.
    /// </returns>
    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (phone.StartsWith('+'))
            return $"+{digits}";

        // Default to US country code if none provided
        return digits.Length == 10
            ? $"+1{digits}"
            : $"+{digits}";
    }
}
