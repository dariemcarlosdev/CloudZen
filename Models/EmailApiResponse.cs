namespace CloudZen.Models;

/// <summary>
/// Response model for successful email API responses.
/// </summary>
/// <remarks>
/// This model is deserialized from the JSON response returned by the <c>SendEmail</c> 
/// Azure Function endpoint when the email is sent successfully.
/// </remarks>
public class EmailApiResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the email was sent successfully.
    /// </summary>
    /// <value><c>true</c> if the email was sent successfully; otherwise, <c>false</c>.</value>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the success message from the API.
    /// </summary>
    /// <value>A confirmation message such as "Email sent successfully."</value>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the unique message identifier from the email provider.
    /// </summary>
    /// <value>The Brevo message ID that can be used for tracking the email delivery.</value>
    public string? MessageId { get; set; }
}
