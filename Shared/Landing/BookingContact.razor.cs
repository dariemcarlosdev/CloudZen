using CloudZen.Models;
using CloudZen.Services.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Shared.Landing;

/// <summary>
/// Code-behind for BookingContact.razor — thin orchestrator holding booking flow state.
/// Composes BookingSidebar, BookingCalendar, BookingTimeSlots, BookingDetailsForm,
/// and BookingConfirmation via parameters and EventCallbacks.
/// </summary>
public partial class BookingContact
{
    [Inject] private IEmailService EmailService { get; set; } = default!;
    [Inject] private IBookingService BookingService { get; set; } = default!;

    // ── State ────────────────────────────────────────────────────────────
    private enum Step { SelectDateTime, EnterDetails, Confirmation }
    private Step currentStep = Step.SelectDateTime;

    private DateTime displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? selectedDate;
    private string? selectedTime;

    private BookingFormModel bookingForm = new();
    private bool isSubmitting;
    private string? errorMessage;
    private string timeZoneLabel = string.Empty;

    protected override void OnInitialized()
    {
        timeZoneLabel = BookingService.GetLocalTimeZoneLabel();
    }

    // ── Step 1 handlers ──────────────────────────────────────────────────

    private void SelectDate(DateTime date)
    {
        selectedDate = date;
        selectedTime = null;
    }

    private void SelectTime(string time) => selectedTime = time;

    private void SetDisplayMonth(DateTime month) => displayMonth = month;

    private void HandleTimeZoneChanged((string Id, string Label) tz)
    {
        timeZoneLabel = tz.Label;
    }

    private void ConfirmDateTime()
    {
        if (selectedDate.HasValue && selectedTime is not null)
            currentStep = Step.EnterDetails;
    }

    private void GoBackToCalendar() => currentStep = Step.SelectDateTime;

    // ── Form submission ──────────────────────────────────────────────────

    private async Task HandleBookingSubmit()
    {
        isSubmitting = true;
        errorMessage = null;

        try
        {
            var subject = $"New Booking: {bookingForm.FullName} — {selectedDate!.Value:MMM dd, yyyy} {selectedTime}";
            var body = $"New meeting booking received:\n\n"
                + $"Name: {bookingForm.FullName}\n"
                + $"Phone: {bookingForm.Phone}\n"
                + $"Email: {bookingForm.Email}\n"
                + $"Business: {bookingForm.BusinessName}\n"
                + $"Date: {selectedDate.Value:dddd, MMMM dd, yyyy}\n"
                + $"Time: {BookingService.FormatSlotRange(selectedTime)}\n"
                + $"Time Zone: {timeZoneLabel}\n"
                + "Opt-In Consent: Yes";

            var result = await EmailService.SendEmailAsync(
                subject,
                body,
                bookingForm.FullName!,
                bookingForm.Email!
            );

            if (result.Success)
            {
                currentStep = Step.Confirmation;
            }
            else
            {
                errorMessage = result.Error ?? "Failed to schedule meeting. Please try again.";
            }
        }
        catch
        {
            errorMessage = "An unexpected error occurred. Please try again later.";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void ResetBooking()
    {
        bookingForm = new BookingFormModel();
        selectedDate = null;
        selectedTime = null;
        displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        currentStep = Step.SelectDateTime;
        errorMessage = null;
        timeZoneLabel = BookingService.GetLocalTimeZoneLabel();
    }
}
