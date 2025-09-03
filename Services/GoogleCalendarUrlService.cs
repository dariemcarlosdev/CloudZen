using System;

namespace CloudZen.Services
{
    public class GoogleCalendarUrlService
    {
        public string CreateConsultationUrl(DateTime? startTime = null, int durationHours = 1)
        {
            var title = Uri.EscapeDataString("CloudZen Consultation");
            var details = Uri.EscapeDataString("Let's discuss how CloudZen can modernize your business.");
            var location = Uri.EscapeDataString("Online Meeting");
            var guests = Uri.EscapeDataString("cloudzen.inc@gmail.com");
            var start = (startTime ?? DateTime.UtcNow.AddDays(1)).ToString("yyyyMMddTHHmmssZ");
            var end = (startTime ?? DateTime.UtcNow.AddDays(1)).AddHours(durationHours).ToString("yyyyMMddTHHmmssZ");
            var url = $"https://calendar.google.com/calendar/render?action=TEMPLATE&text={title}&details={details}&location={location}&dates={start}/{end}&add={guests}";
            return url;
        }
    }
}
