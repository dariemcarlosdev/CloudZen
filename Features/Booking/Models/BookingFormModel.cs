using System.ComponentModel.DataAnnotations;

namespace CloudZen.Features.Booking.Models;

/// <summary>
/// Represents the data model for the booking/scheduling form submission.
/// Used in the BookingContact component (Step 2: Enter Details).
/// </summary>
public class BookingFormModel
{
    [Required(ErrorMessage = "Please enter your full name")]
    [StringLength(100, ErrorMessage = "Name is too long (max 100 characters)")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Please enter your phone number")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter your business name")]
    [StringLength(200, ErrorMessage = "Business name is too long (max 200 characters)")]
    public string? BusinessName { get; set; }

    [StringLength(500, ErrorMessage = "Reason is too long (max 500 characters)")]
    public string? Reason { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm your consent to continue")]
    public bool OptInConsent { get; set; }
}
