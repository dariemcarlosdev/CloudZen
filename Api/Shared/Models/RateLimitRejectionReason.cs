namespace CloudZen.Api.Shared.Models;

/// <summary>
/// Specifies the reason for a rate limit rejection.
/// </summary>
/// <remarks>
/// This enumeration is used for logging, metrics, and determining the appropriate
/// error message to return to the client.
/// </remarks>
public enum RateLimitRejectionReason
{
    /// <summary>
    /// The request was rejected because the rate limit was exceeded.
    /// </summary>
    /// <remarks>
    /// This is the most common rejection reason, indicating the client has made
    /// too many requests within the configured time window.
    /// </remarks>
    RateLimitExceeded,

    /// <summary>
    /// The request was rejected because the circuit breaker is in the open state.
    /// </summary>
    /// <remarks>
    /// This indicates the system has detected a pattern of failures and is
    /// temporarily blocking requests to allow recovery.
    /// </remarks>
    CircuitBreakerOpen,

    /// <summary>
    /// The request was rejected due to a timeout.
    /// </summary>
    /// <remarks>
    /// This indicates the request took too long to process and was terminated.
    /// </remarks>
    Timeout
}
