using CloudZen.Models;
using CloudZen.Models.Options;
using CloudZen.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudZen.Services;

/// <summary>
/// Chatbot service implementation that sends messages through the Azure Functions API backend.
/// </summary>
/// <remarks>
/// <para>
/// This is the recommended and secure approach for Blazor WebAssembly applications,
/// ensuring that the Anthropic API key remains on the server side and is never exposed to the client browser.
/// </para>
/// <para>
/// The service communicates with the <c>Chat</c> Azure Function endpoint, which handles:
/// <list type="bullet">
///   <item><description>Rate limiting to prevent abuse</description></item>
///   <item><description>Input validation and sanitization</description></item>
///   <item><description>Secure API key management</description></item>
///   <item><description>Proxying to the Anthropic (Claude) API</description></item>
/// </list>
/// </para>
/// </remarks>
public class ChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly ChatbotOptions _options;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        HttpClient httpClient,
        IOptions<ChatbotOptions> options,
        ILogger<ChatbotService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<ChatResult> SendMessageAsync(List<ChatMessage> messages)
    {
        try
        {
            var request = new
            {
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
            };

            var endpoint = _options.ChatUrl;
            _logger.LogInformation("Sending chat request to {Endpoint}", endpoint);

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ChatApiResponse>();

                if (result?.Success == true && !string.IsNullOrEmpty(result.Reply))
                {
                    _logger.LogInformation("Chat response received successfully.");
                    return ChatResult.Ok(result.Reply);
                }

                return ChatResult.Fail(result?.Error ?? "Failed to get a response.");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Chat API returned error: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);

                try
                {
                    var errorResult = JsonSerializer.Deserialize<ChatApiResponse>(errorContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return ChatResult.Fail(errorResult?.Error ?? "Failed to get a response.");
                }
                catch
                {
                    return ChatResult.Fail($"Failed to get a response. Status: {response.StatusCode}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error in chat service: {Message}", ex.Message);
            return ChatResult.Fail("Unable to connect to the chat service. Please check your internet connection.");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout in chat service after {TimeoutSeconds} seconds", _options.TimeoutSeconds);
            return ChatResult.Fail("Request timed out. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in chat service: {Message}", ex.Message);
            return ChatResult.Fail("Something went wrong. Please try again later.");
        }
    }

    /// <summary>
    /// Internal response model matching the Azure Function's ChatResponse.
    /// </summary>
    private class ChatApiResponse
    {
        public bool Success { get; set; }
        public string? Reply { get; set; }
        public string? Error { get; set; }
    }
}
