using CloudZen.Features.Booking.Models;

namespace CloudZen.Features.Booking.Services;

/// <summary>
/// Sends appointment requests (book, cancel, reschedule) to the n8n webhook endpoint.
/// </summary>
public interface IAppointmentService
{
    /// <summary>
    /// Books a new appointment via the n8n workflow.
    /// </summary>
    /// <param name="request">The booking details.</param>
    /// <returns>An <see cref="AppointmentResponse"/> with status code and result.</returns>
    Task<AppointmentResponse> BookAsync(BookAppointmentRequest request);

    /// <summary>
    /// Cancels an existing appointment via the n8n workflow.
    /// </summary>
    /// <param name="request">The cancellation details.</param>
    /// <returns>An <see cref="AppointmentResponse"/> with status code and result.</returns>
    Task<AppointmentResponse> CancelAsync(CancelAppointmentRequest request);

    /// <summary>
    /// Reschedules an existing appointment to a new date/time via the n8n workflow.
    /// </summary>
    /// <param name="request">The reschedule details.</param>
    /// <returns>An <see cref="AppointmentResponse"/> with status code and result.</returns>
    Task<AppointmentResponse> RescheduleAsync(RescheduleAppointmentRequest request);
}
