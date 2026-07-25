namespace CloudZen.Features.Contact.Models;

/// <summary>
/// Response model for email API error responses.
/// </summary>
/// <remarks>
/// This model is deserialized from the JSON response returned by the <c>SendEmail</c> 
/// Azure Function endpoint when an error occurs (4xx or 5xx status codes).
/// </remarks>
public class EmailApiErrorResponse
{
    /// <summary>
    /// Gets or sets the error message describing what went wrong.
    /// </summary>
    /// <value>
    /// A user-friendly error message that can be displayed to the user,
    /// such as "Email is required." or "Rate limit exceeded."
    /// </value>
    public string? Error { get; set; }
}
