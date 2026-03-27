using CloudZen.Features.Chat.Models;

namespace CloudZen.Features.Chat.Services;

/// <summary>
/// Interface for the chatbot service that sends messages through the Azure Functions API backend.
/// </summary>
/// <remarks>
/// This is the recommended and secure approach for Blazor WebAssembly applications,
/// ensuring that the Anthropic API key remains on the server side.
/// </remarks>
public interface IChatbotService
{
    /// <summary>
    /// Sends the conversation history to the backend and receives the assistant's reply.
    /// </summary>
    /// <param name="messages">The full conversation history (user and assistant messages).</param>
    /// <returns>A <see cref="ChatResult"/> indicating success with the reply or failure with an error.</returns>
    Task<ChatResult> SendMessageAsync(List<ChatMessage> messages);
}

/// <summary>
/// Result of a chatbot send operation.
/// </summary>
public class ChatResult
{
    public bool Success { get; set; }
    public string? Reply { get; set; }
    public string? Error { get; set; }

    public static ChatResult Ok(string reply) => new() { Success = true, Reply = reply };
    public static ChatResult Fail(string error) => new() { Success = false, Error = error };
}
