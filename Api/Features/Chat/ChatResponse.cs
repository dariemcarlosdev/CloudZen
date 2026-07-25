namespace CloudZen.Api.Features.Chat;

/// <summary>
/// Response model returned by the Chat function.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Whether the chat request was processed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The assistant's reply text.
    /// </summary>
    public string Reply { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    public string? Error { get; set; }
}
