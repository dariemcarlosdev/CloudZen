namespace CloudZen.Features.Chat.Models;

/// <summary>
/// Represents a single message in the chatbot conversation.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The role of the message sender. Either "user" or "assistant".
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new user message.
    /// </summary>
    public static ChatMessage User(string content) => new() { Role = "user", Content = content };

    /// <summary>
    /// Creates a new assistant message.
    /// </summary>
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
}
