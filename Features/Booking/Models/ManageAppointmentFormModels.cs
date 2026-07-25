using System.ComponentModel.DataAnnotations;

namespace CloudZen.Features.Booking.Models;

/// <summary>
/// Form model for cancelling an appointment.
/// </summary>
public class CancelFormModel
{
    [Required(ErrorMessage = "Please enter your booking ID")]
    [RegularExpression(@"^APT-[A-Z0-9]{8}-[A-Z0-9]{4}$",
        ErrorMessage = "Please enter a valid booking ID (e.g., APT-MN7O3825-TMVP)")]
    public string? BookingId { get; set; }

    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }
}

/// <summary>
/// Form model for rescheduling an appointment.
/// </summary>
public class RescheduleFormModel
{
    [Required(ErrorMessage = "Please enter your booking ID")]
    [RegularExpression(@"^APT-[A-Z0-9]{8}-[A-Z0-9]{4}$",
        ErrorMessage = "Please enter a valid booking ID (e.g., APT-MN7O3825-TMVP)")]
    public string? BookingId { get; set; }

    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }
}
