using CloudZen.Api.Models;
using CloudZen.Api.Security;
using CloudZen.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CloudZen.Api.Functions;

/// <summary>
/// Azure Function to handle chatbot requests by proxying to the Anthropic (Claude) API.
/// </summary>
/// <remarks>
/// <para>
/// This function serves as a secure backend for the Blazor WebAssembly chatbot widget,
/// ensuring that the Anthropic API key and the full knowledge base/system prompt remain
/// server-side and are never exposed to the client browser.
/// </para>
/// <para>
/// Security features:
/// <list type="bullet">
///   <item><description>API key stored in Azure Key Vault / environment variables only</description></item>
///   <item><description>Rate limiting to prevent abuse</description></item>
///   <item><description>Input validation and size limiting</description></item>
///   <item><description>CORS and security headers</description></item>
///   <item><description>Knowledge base and system prompt never sent to client</description></item>
/// </list>
/// </para>
/// </remarks>
public class ChatFunction(
    ILogger<ChatFunction> logger,
    IConfiguration config,
    IRateLimiterService rateLimiter,
    CorsSettings corsSettings,
    IHttpClientFactory httpClientFactory)
{
    private readonly ILogger<ChatFunction> _logger = logger;
    private readonly IConfiguration _config = config;
    private readonly IRateLimiterService _rateLimiter = rateLimiter;
    private readonly CorsSettings _corsSettings = corsSettings;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const string DefaultModel = "claude-sonnet-4-20250514";
    private const int MaxTokens = 200;
    private const int MaxRequestBodySize = 15000;
    private const int MaxMessages = 10;
    private const int MaxConversationHistoryMessages = 6;
    private const int MaxMessageContentLength = 500;
    private const int MaxReplyLength = 500;

    private static readonly JsonSerializerOptions ChatJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 10
    };

    #region Knowledge Base & System Prompt

    private const string KnowledgeBase = """
        You are the CloudZen AI Assistant. You represent CloudZen, a technology consultancy that helps small businesses modernize, automate, and grow using technology — with zero jargon.

        ---

        ## IDENTITY & BRAND

        - **Name**: CloudZen
        - **Tagline**: "You don't need to speak tech. You just need results."
        - **Sub-tagline**: "Technology That Works - So You Can Focus on Growing"
        - **Type**: Technology consultancy with a trusted specialist network
        - **Target audience**: Small business owners who are non-technical and frustrated with slow, outdated systems
        - **Email**: cloudzen.inc@gmail.com
        - **Founded**: Active as of 2026
        - **Copyright**: © 2026 CloudZen. All Rights Reserved.
        - **Socials**: LinkedIn, GitHub
        - **Positioning**: Personalized attention and direct communication + depth of a handpicked specialist team

        ---

        ## MISSION & VALUES

        - **Mission**: Help small businesses modernize outdated systems, automate daily tasks, and build tools that make life easier — no jargon, no guesswork.
        - **Core promise**: You focus on your customers. CloudZen handles the technology that powers your business.
        - **Values**:
          - Honesty and clear communication
          - Practical, measurable results
          - No cookie-cutter approaches — every solution is tailored
          - Direct communication (no middlemen, no phone trees)
          - Bring in trusted experts only when genuinely needed

        ---

        ## KEY DIFFERENTIATORS

        1. **One Point of Contact**: Clients get a dedicated point of contact — no phone trees, no account managers, straight answers.
        2. **Trusted Specialist Network**: When a project needs extra skills, trusted handpicked experts are brought in — clients get full-team depth without the overhead.
        3. **Real-World Results**: Proven track record — helped businesses ditch slow systems, save hours every week, and win more customers.
        4. **Tailored Solutions**: No templates or generic approaches. Every solution is built around specific business goals, processes, and customers.
        5. **Jargon-Free Communication**: Built for non-technical business owners. No tech speak, no runaround.

        ---

        ## SERVICES

        ### 1. Custom Software Development
        - Systems and applications tailored to how a business actually works
        - Designed around client goals, processes, and customers
        - No generic templates — grows with the business

        ### 2. Cloud Hosting & Infrastructure
        - Move businesses online with confidence
        - Cut costs, gain flexibility, support growth
        - No tech overwhelm or hidden surprises
        - Expertise in Azure (Microsoft Azure Static Web Apps, cloud-native architecture)

        ### 3. Legacy System Modernization
        - Transition smoothly from outdated systems to modern solutions
        - Keep the business running during the transition
        - Set up for long-term future growth

        ### 4. DevOps / Faster Delivery (CI/CD)
        - Speed up delivery of updates and new services
        - Reduce delays and streamline operations
        - Give customers better experiences every time

        ### 5. Data Dashboards & Business Intelligence
        - Visual dashboards that turn raw data into actionable decisions
        - See what matters, when it matters
        - Stop guessing — start acting on real insights

        ### 6. AI Automation
        - Automate repetitive tasks so the owner can focus on what matters
        - Catch issues early with AI-powered monitoring
        - Deliver smarter customer experiences
        - Practical, grounded AI — not hype

        ### 7. Extended Specialist Expertise
        - Bring in trusted specialists to fill skill gaps
        - Complete end-to-end solutions without the hiring headache
        - Vetted experts who integrate seamlessly into the project

        ### 8. Quality Assurance & Launch Support
        - Thorough testing so solutions work reliably from day one
        - No crossed fingers at launch
        - Peace of mind for both business owner and their customers

        ### 9. Agile / Iterative Delivery
        - Client input shapes every stage
        - Work in focused cycles — quick visible progress
        - Constant feedback loop, no surprises at the end

        ---

        ## CASE STUDIES / PORTFOLIO

        ### Project 1: Assessment Platform Modernization (COMPLETED)
        - **Client type**: Education sector — MDCPS (Miami-Dade County Public Schools)
        - **Platform**: WPBT Assessment Services
        - **Problem**: Legacy, outdated assessment platform that was slow and not scalable
        - **Solution**: Full migration from legacy systems to modern technology — improved scalability, security, and user experience
        - **Key results**:
          - Streamlined assessment cycle cutting delivery speed by **50%**
          - Delivered a responsive, modular interface for principals and administrators
        - **Category**: Customer Success

        ### Project 2: SAP Data Pipeline Optimization (COMPLETED)
        - **Problem**: Full daily data loads were slow and creating performance bottlenecks in the Data Warehouse
        - **Solution**: Engineered a delta-based data processing pipeline for SAP Maintenance Data — replaced full daily loads with smart incremental data extraction and lookup-based deduplication
        - **Key results**:
          - SAP data ingestion **70% faster**
          - Resolved performance bottlenecks in the downstream Data Warehouse (DW)
        - **Category**: Customer Success

        ### Project 3: AI Menu Optimization (IN PROGRESS)
        - **Product name**: Smart Menu Optimizer
        - **Type**: Cloud-native, AI-powered recommendation engine
        - **Target users**: Restaurant owners
        - **Problem**: Restaurant owners make menu decisions without data-driven insights, leading to suboptimal profit and food waste
        - **Solution**: AI recommendation engine that helps restaurants make data-driven decisions about their menu based on real-time inventory, profitability, and customer preferences
        - **Key results (projected)**:
          - Increase profit margin per order by **+18%** through optimized menu recommendations
          - Reduce food waste by **22%** by aligning menu offerings with real-time inventory
        - **Code**: Open source / viewable
        - **Category**: Innovation Project

        ---

        ## HOW IT WORKS / PROCESS

        1. **Free Consultation**: Schedule a no-commitment consultation to discuss the business problem
        2. **Discovery**: CloudZen listens, asks questions, and understands the specific goals and pain points
        3. **Proposal**: A practical, tailored solution plan — no jargon, clear outcomes
        4. **Iterative Build**: Work in focused cycles with regular feedback — client sees progress quickly
        5. **Testing & Launch**: Everything is tested thoroughly before going live
        6. **Ongoing Support**: Available for continued partnership after launch

        ---

        ## IDEAL CLIENT PROFILE

        - Small business owners
        - Non-technical (don't need to understand the tech)
        - Frustrated with slow, outdated systems
        - Drowning in manual, repetitive tasks
        - Want clear communication and real results
        - Don't want to deal with big agencies or manage large teams
        - Need technology that actually moves the needle for their business

        ---

        ## PAIN POINTS CLOUDZEN SOLVES

        - "Our systems are too slow and outdated"
        - "We waste hours every week on manual tasks"
        - "We don't understand what our data is telling us"
        - "We've worked with agencies and gotten no results / lots of confusion"
        - "We need to modernize but can't afford downtime"
        - "We want AI but don't know where to start"
        - "We need a trustworthy tech partner who speaks plain English"

        ---

        ## TECHNOLOGY EXPERTISE (inferred from projects and services)

        - **Cloud**: Microsoft Azure (Azure Static Web Apps, cloud-native apps)
        - **AI/ML**: Recommendation engines, automation, AI-powered tools
        - **Data**: SAP data pipelines, delta processing, Data Warehouses, dashboards
        - **Frontend**: Blazor WebAssembly, modern responsive UI
        - **DevOps**: CI/CD pipelines, deployment automation
        - **Backend**: Custom APIs, system integrations
        - **Legacy modernization**: Migration from outdated stacks to modern platforms

        ---

        ## CONTACT & BOOKING

        - **Email**: cloudzen.inc@gmail.com
        - **Response time**: Within 24 hours
        - **Contact form**: Available on the website (name, email, subject, message — max 500 chars)
        - **First step**: Book a free consultation via the website
        - **Privacy**: No spam, ever. Secure & encrypted contact form.

        ---

        ## TONE & COMMUNICATION STYLE

        When responding as the CloudZen assistant:
        - Be warm, approachable, and jargon-free
        - Focus on business outcomes, not technical details
        - Be honest — if something is outside scope, say so clearly
        - Keep answers concise and practical
        - Always offer to connect the user with CloudZen for a real conversation
        - Never oversell — let the results speak
        - Use plain English. The audience is non-technical business owners.
        """;

    private static readonly string SystemPrompt = $"""
        {KnowledgeBase}

        You are CloudZen's friendly AI assistant embedded on the website. Your job is to:
        1. Answer questions about CloudZen's services, process, projects, and values
        2. Help visitors understand if CloudZen is the right fit for their needs
        3. Guide interested visitors toward booking a free consultation
        4. Be warm, clear, and jargon-free at all times

        IMPORTANT GUIDELINES:
        - HARD LIMIT: Every response MUST be under 500 characters total. This is non-negotiable. Count carefully.
        - Keep responses to 1-2 sentences max. Be brief and direct.
        - After answering a question, ALWAYS suggest a next step — either booking a free consultation or emailing cloudzen.inc@gmail.com.
        - If the visitor asks more than 2-3 questions, proactively suggest: "I can share the basics, but every business is unique — would you like to book a free consultation to discuss your specific situation?"
        - Do NOT provide detailed technical advice, implementation specifics, or lengthy explanations. Keep it high-level and redirect to a real conversation.
        - If asked about pricing, timelines, or project-specific details, say that those depend on the specific situation and encourage them to book a free consultation.
        - If asked something outside your knowledge, say so honestly and suggest they email cloudzen.inc@gmail.com.
        - Do NOT engage with off-topic conversations, jokes, roleplay, or anything unrelated to CloudZen's services. Politely redirect.
        - You are NOT a general-purpose AI assistant. Only answer questions relevant to CloudZen.
        """;

    #endregion

    /// <summary>
    /// HTTP POST endpoint to handle chatbot messages by proxying to the Anthropic Claude API.
    /// Also handles OPTIONS preflight requests for CORS.
    /// </summary>
    /// <param name="req">The HTTP request containing a <see cref="ChatRequest"/> JSON body.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing:
    /// <list type="bullet">
    ///   <item><description><b>200 OK</b> - Chat response with assistant reply</description></item>
    ///   <item><description><b>204 No Content</b> - For CORS preflight requests</description></item>
    ///   <item><description><b>400 Bad Request</b> - Invalid request body or validation failure</description></item>
    ///   <item><description><b>429 Too Many Requests</b> - Rate limit exceeded</description></item>
    ///   <item><description><b>500 Internal Server Error</b> - AI service configuration error or upstream failure</description></item>
    /// </list>
    /// </returns>
    [Function("Chat")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "chat")] HttpRequest req)
    {
        // Add CORS headers to all responses
        req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);

        // Handle CORS preflight requests
        if (req.IsCorsPreflightRequest())
        {
            return new StatusCodeResult(StatusCodes.Status204NoContent);
        }

        // Add security headers to response
        req.HttpContext.Response.AddSecurityHeaders();

        // Get client IP for rate limiting and logging
        var clientIp = req.GetClientIpAddress();
        var correlationId = req.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ClientIp"] = InputValidator.SanitizeForLogging(clientIp)
        });

        _logger.LogInformation("Chat function triggered from {ClientIp}", InputValidator.SanitizeForLogging(clientIp));

        try
        {
            // Check rate limit
            var rateLimitResult = await _rateLimiter.TryAcquireAsync(clientIp, "chat");
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientIp}", InputValidator.SanitizeForLogging(clientIp));

                req.HttpContext.Response.Headers.TryAdd("Retry-After", rateLimitResult.RetryAfter?.TotalSeconds.ToString("F0") ?? "60");

                return new ObjectResult(new ChatResponse { Success = false, Error = rateLimitResult.Message })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
            }

            // Read and parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Empty chat request body received.");
                return new BadRequestObjectResult(new ChatResponse { Success = false, Error = "Request body is required." });
            }

            if (requestBody.Length > MaxRequestBodySize)
            {
                _logger.LogWarning("Chat request body too large: {Size} bytes", requestBody.Length);
                return new BadRequestObjectResult(new ChatResponse { Success = false, Error = "Request body too large." });
            }

            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, ChatJsonOptions);

            if (chatRequest?.Messages == null || chatRequest.Messages.Count == 0)
            {
                _logger.LogWarning("Chat request has no messages.");
                return new BadRequestObjectResult(new ChatResponse { Success = false, Error = "At least one message is required." });
            }

            if (chatRequest.Messages.Count > MaxMessages)
            {
                _logger.LogWarning("Chat request has too many messages: {Count}", chatRequest.Messages.Count);
                return new BadRequestObjectResult(new ChatResponse { Success = false, Error = "Too many messages in conversation." });
            }

            // Validate each message
            foreach (var msg in chatRequest.Messages)
            {
                if (string.IsNullOrWhiteSpace(msg.Role) || string.IsNullOrWhiteSpace(msg.Content))
                {
                    return new BadRequestObjectResult(new ChatResponse { Success = false, Error = "Each message must have a role and content." });
                }

                if (msg.Role is not ("user" or "assistant"))
                {
                    return new BadRequestObjectResult(new ChatResponse { Success = false, Error = "Message role must be 'user' or 'assistant'." });
                }

                // Only enforce content length on user messages — assistant messages are
                // generated by the API itself and may exceed the user input limit.
                if (msg.Role == "user" && msg.Content.Length > MaxMessageContentLength)
                {
                    return new BadRequestObjectResult(new ChatResponse { Success = false, Error = $"Message content is too long. Maximum {MaxMessageContentLength} characters." });
                }
            }

            // Get Anthropic API key from configuration
            var apiKey = _config["ANTHROPIC_API_KEY"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Anthropic API key is not configured. Ensure ANTHROPIC_API_KEY is set in environment variables or Key Vault.");
                return new ObjectResult(new ChatResponse { Success = false, Error = "Chat service is not configured properly." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            // Call Anthropic API
            var reply = await CallAnthropicApiAsync(chatRequest, apiKey);

            _logger.LogInformation("Chat response generated successfully.");

            return new OkObjectResult(new ChatResponse
            {
                Success = true,
                Reply = reply
            });
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("billing error", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Anthropic API billing/quota issue: {Message}", ex.Message);
            return new ObjectResult(new ChatResponse { Success = false, Error = "The AI service is temporarily unavailable due to a configuration issue. Please contact the administrator." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Anthropic API rate limited: {Message}", ex.Message);
            return new ObjectResult(new ChatResponse { Success = false, Error = "The AI service is currently busy. Please try again in a moment." })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Anthropic API: {Message}", ex.Message);
            return new ObjectResult(new ChatResponse { Success = false, Error = "Unable to reach the AI service. Please try again later." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Anthropic API.");
            return new ObjectResult(new ChatResponse { Success = false, Error = "The AI service took too long to respond. Please try again." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error: {Message}", ex.Message);
            return new BadRequestObjectResult(new ChatResponse { Success = false, Error = "Invalid request format." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in chat function: {Message}", ex.Message);
            return new ObjectResult(new ChatResponse { Success = false, Error = "Something went wrong. Please try again later." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <summary>
    /// Calls the Anthropic Messages API with the conversation history and system prompt.
    /// </summary>
    private async Task<string> CallAnthropicApiAsync(ChatRequest chatRequest, string apiKey)
    {
        var httpClient = _httpClientFactory.CreateClient("SecureClient");

        // Only send the most recent messages to Anthropic to limit token consumption.
        // This prevents long conversations from sending excessive context.
        var recentMessages = chatRequest.Messages
            .TakeLast(MaxConversationHistoryMessages)
            .Select(m => new { role = m.Role, content = m.Content })
            .ToArray();

        // Ensure the first message is always from the user (Anthropic requirement)
        if (recentMessages.Length > 0 && recentMessages[0].role != "user")
        {
            recentMessages = recentMessages.Skip(1).ToArray();
        }

        var anthropicRequest = new
        {
            model = DefaultModel,
            max_tokens = MaxTokens,
            system = SystemPrompt,
            messages = recentMessages
        };

        var json = JsonSerializer.Serialize(anthropicRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl);
        request.Content = content;
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        var response = await httpClient.SendAsync(request);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic API returned {StatusCode}: {Body}", response.StatusCode, responseBody);

            // Parse error type from Anthropic response for specific handling
            var errorType = string.Empty;
            try
            {
                using var errorDoc = JsonDocument.Parse(responseBody);
                if (errorDoc.RootElement.TryGetProperty("error", out var errorElement) &&
                    errorElement.TryGetProperty("type", out var typeElement))
                {
                    errorType = typeElement.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Ignore parse failures on error body
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                (responseBody.Contains("credit balance is too low", StringComparison.OrdinalIgnoreCase) ||
                 errorType == "invalid_request_error" && responseBody.Contains("credit", StringComparison.OrdinalIgnoreCase)))
            {
                throw new HttpRequestException("Anthropic API billing error: insufficient credits. Please check your Anthropic plan and billing.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                errorType == "rate_limit_error")
            {
                throw new HttpRequestException("Anthropic API rate limit exceeded. Please try again later.");
            }

            throw new HttpRequestException($"Anthropic API returned {response.StatusCode}");
        }

        // Parse the Anthropic response to extract the text
        using var doc = JsonDocument.Parse(responseBody);
        var contentArray = doc.RootElement.GetProperty("content");
        var replyBuilder = new StringBuilder();

        foreach (var block in contentArray.EnumerateArray())
        {
            if (block.TryGetProperty("text", out var textElement))
            {
                replyBuilder.Append(textElement.GetString());
            }
        }

        var reply = replyBuilder.ToString();

        if (string.IsNullOrEmpty(reply))
        {
            _logger.LogWarning("Anthropic API returned empty content.");
            return "Sorry, I couldn't generate a response. Please try again.";
        }

        // Truncate reply if it exceeds the max length, cutting at the last sentence boundary
        if (reply.Length > MaxReplyLength)
        {
            _logger.LogWarning("Anthropic reply exceeded {MaxLength} chars ({ActualLength}). Truncating.", MaxReplyLength, reply.Length);
            var truncated = reply[..MaxReplyLength];
            var lastSentenceEnd = truncated.LastIndexOfAny(['.', '!', '?']);
            reply = lastSentenceEnd > 0 ? truncated[..(lastSentenceEnd + 1)] : truncated + "…";
        }

        return reply;
    }
}
