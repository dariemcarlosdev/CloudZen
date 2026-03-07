using System.ComponentModel.DataAnnotations;

namespace CloudZen.Models;

/// <summary>
/// Represents the data model for the contact form submission.
/// </summary>
public class ContactFormModel
{
    [Required(ErrorMessage = "Please enter your name")]
    [StringLength(100, ErrorMessage = "Name is too long (max 100 characters)")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter a subject")]
    [StringLength(200, ErrorMessage = "Subject is too long (max 200 characters)")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Please enter your message")]
    [StringLength(500, ErrorMessage = "Message is too long (max 500 characters)")]
    public string? Message { get; set; }
}
