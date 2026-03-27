namespace CloudZen.Features.Booking.Services;

/// <summary>
/// Interface for generating Google Calendar pre-filled URLs for consultations.
/// </summary>
public interface IGoogleCalendarUrlService
{
    string CreateConsultationUrl(DateTime? startTime = null, int durationHours = 1);
}
