namespace CloudZen.Api.Models;

/// <summary>
/// Represents the result of a rate limit check operation.
/// </summary>
/// <remarks>
/// Use the static factory methods <see cref="Allowed"/> and <see cref="Limited"/> to create instances.
/// This follows the result pattern for clear success/failure handling.
/// </remarks>
public class RateLimitResult
{
    /// <summary>
    /// Gets a value indicating whether the request is allowed to proceed.
    /// </summary>
    /// <value><c>true</c> if the request is within rate limits; otherwise, <c>false</c>.</value>
    public bool IsAllowed { get; init; }

    /// <summary>
    /// Gets the number of remaining requests allowed in the current time window.
    /// </summary>
    /// <value>The count of remaining permits. Only meaningful when <see cref="IsAllowed"/> is <c>true</c>.</value>
    public int RemainingRequests { get; init; }

    /// <summary>
    /// Gets the duration the client should wait before retrying.
    /// </summary>
    /// <value>
    /// The recommended retry delay, or <c>null</c> if the request was allowed.
    /// This value should be returned in the <c>Retry-After</c> HTTP header.
    /// </value>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Gets a user-friendly message describing the rate limit status.
    /// </summary>
    /// <value>
    /// A message suitable for returning to the client, such as 
    /// "Rate limit exceeded. Try again in 60 seconds."
    /// </value>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the reason for rate limit rejection.
    /// </summary>
    /// <value>
    /// The specific reason the request was rejected, or <c>null</c> if allowed.
    /// Used for logging and metrics purposes.
    /// </value>
    public RateLimitRejectionReason? RejectionReason { get; init; }

    /// <summary>
    /// Creates a successful rate limit result indicating the request is allowed.
    /// </summary>
    /// <param name="remaining">The number of remaining requests in the current window.</param>
    /// <returns>A <see cref="RateLimitResult"/> with <see cref="IsAllowed"/> set to <c>true</c>.</returns>
    public static RateLimitResult Allowed(int remaining) => new()
    {
        IsAllowed = true,
        RemainingRequests = remaining
    };

    /// <summary>
    /// Creates a rate limit result indicating the request was rejected.
    /// </summary>
    /// <param name="retryAfter">The duration the client should wait before retrying.</param>
    /// <param name="reason">The reason for rejection. Defaults to <see cref="RateLimitRejectionReason.RateLimitExceeded"/>.</param>
    /// <returns>A <see cref="RateLimitResult"/> with <see cref="IsAllowed"/> set to <c>false</c> and appropriate message.</returns>
    /// <remarks>
    /// The <see cref="Message"/> property is automatically populated based on the rejection reason:
    /// <list type="bullet">
    ///   <item><description><see cref="RateLimitRejectionReason.CircuitBreakerOpen"/>: "Service temporarily unavailable. Please try again later."</description></item>
    ///   <item><description><see cref="RateLimitRejectionReason.Timeout"/>: "Request timed out. Please try again."</description></item>
    ///   <item><description><see cref="RateLimitRejectionReason.RateLimitExceeded"/>: "Rate limit exceeded. Try again in {seconds} seconds."</description></item>
    /// </list>
    /// </remarks>
    public static RateLimitResult Limited(TimeSpan retryAfter, RateLimitRejectionReason reason = RateLimitRejectionReason.RateLimitExceeded) => new()
    {
        IsAllowed = false,
        RetryAfter = retryAfter,
        RejectionReason = reason,
        Message = reason switch
        {
            RateLimitRejectionReason.CircuitBreakerOpen => "Service temporarily unavailable. Please try again later.",
            RateLimitRejectionReason.Timeout => "Request timed out. Please try again.",
            _ => $"Rate limit exceeded. Try again in {retryAfter.TotalSeconds:F0} seconds."
        }
    };
}
