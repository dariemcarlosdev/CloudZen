using CloudZen.Api.Shared.Models;
using System.Threading.RateLimiting;

namespace CloudZen.Api.Shared.Services;

/// <summary>
/// Service interface for handling rate limiting of API endpoints.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface provide protection against abuse and DDoS attacks
/// by limiting the number of requests a client can make within a time window.
/// </para>
/// <para>
/// Rate limiting is typically applied per client (identified by IP address or API key)
/// and per endpoint, allowing different limits for different operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyFunction
/// {
///     private readonly IRateLimiterService _rateLimiter;
///     
///     public async Task&lt;IActionResult&gt; Run(HttpRequest req)
///     {
///         var clientIp = req.GetClientIpAddress();
///         var result = await _rateLimiter.TryAcquireAsync(clientIp, "my-endpoint");
///         
///         if (!result.IsAllowed)
///         {
///             return new StatusCodeResult(429); // Too Many Requests
///         }
///         
///         // Process the request...
///     }
/// }
/// </code>
/// </example>
public interface IRateLimiterService
{
    /// <summary>
    /// Attempts to acquire a permit for the specified client and endpoint.
    /// </summary>
    /// <param name="clientIdentifier">
    /// A unique identifier for the client making the request.
    /// Typically an IP address, API key, or user ID.
    /// </param>
    /// <param name="endpoint">
    /// The name or path of the endpoint being accessed.
    /// Used to apply different rate limits to different operations.
    /// </param>
    /// <returns>
    /// A <see cref="RateLimitResult"/> indicating whether the request is allowed:
    /// <list type="bullet">
    ///   <item><description><see cref="RateLimitResult.IsAllowed"/> = <c>true</c>: Request can proceed</description></item>
    ///   <item><description><see cref="RateLimitResult.IsAllowed"/> = <c>false</c>: Request should be rejected with 429 status</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently from multiple requests.
    /// Each call consumes one permit from the client's allowance for the specified endpoint.
    /// </remarks>
    Task<RateLimitResult> TryAcquireAsync(string clientIdentifier, string endpoint);

    /// <summary>
    /// Gets the current rate limiting statistics for a specific client and endpoint combination.
    /// </summary>
    /// <param name="clientIdentifier">The unique identifier for the client.</param>
    /// <param name="endpoint">The endpoint to get statistics for.</param>
    /// <returns>
    /// A <see cref="RateLimiterStatistics"/> object containing current permit availability,
    /// or <c>null</c> if no rate limiter exists for the specified combination.
    /// </returns>
    /// <remarks>
    /// This method is useful for monitoring and debugging rate limiting behavior.
    /// The statistics reflect the current state and may change immediately after the call.
    /// </remarks>
    RateLimiterStatistics? GetStatistics(string clientIdentifier, string endpoint);
}
