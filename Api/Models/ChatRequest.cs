namespace CloudZen.Api.Models;

/// <summary>
/// Request model for the Chat function.
/// </summary>
/// <remarks>
/// Contains the conversation history to be forwarded to the AI provider.
/// The system prompt and knowledge base are managed server-side and never exposed to the client.
/// </remarks>
public class ChatRequest
{
    /// <summary>
    /// The conversation messages exchanged between the user and assistant.
    /// </summary>
    /// <remarks>
    /// Each message must have a <c>role</c> ("user" or "assistant") and <c>content</c> (the message text).
    /// Messages should be in chronological order.
    /// </remarks>
    public List<ChatMessageItem> Messages { get; set; } = [];
}

/// <summary>
/// A single message in a chat conversation.
/// </summary>
public class ChatMessageItem
{
    /// <summary>
    /// The role of the message sender. Either "user" or "assistant".
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
