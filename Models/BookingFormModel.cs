using System.ComponentModel.DataAnnotations;

namespace CloudZen.Models;

/// <summary>
/// Represents the data model for the booking/scheduling form submission.
/// Used in the BookingContact component (Step 2: Enter Details).
/// </summary>
public class BookingFormModel
{
    [Required(ErrorMessage = "Full Name is required")]
    [StringLength(100, ErrorMessage = "Name is too long (max 100 characters)")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Business Name is required")]
    [StringLength(200, ErrorMessage = "Business Name is too long (max 200 characters)")]
    public string? BusinessName { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must provide opt-in consent")]
    public bool OptInConsent { get; set; }
}
