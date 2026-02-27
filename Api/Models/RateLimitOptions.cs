namespace CloudZen.Api.Models;

/// <summary>
/// Configuration options for rate limiting and resilience policies.
/// </summary>
/// <remarks>
/// <para>
/// This class is used with the ASP.NET Core Options pattern to configure the 
/// <see cref="Services.PollyRateLimiterService"/>. Settings can be configured in <c>appsettings.json</c>
/// under the <c>RateLimiting</c> section.
/// </para>
/// <para>
/// Example configuration:
/// <code>
/// {
///   "RateLimiting": {
///     "PermitLimit": 10,
///     "WindowSeconds": 60,
///     "QueueLimit": 0,
///     "EnableCircuitBreaker": true
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public class RateLimitOptions
{
    /// <summary>
    /// The configuration section name for rate limiting options.
    /// </summary>
    /// <value>The string "RateLimiting" used to bind configuration.</value>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Gets or sets the number of requests allowed per time window.
    /// </summary>
    /// <value>The maximum number of permits per window. Defaults to 10.</value>
    /// <remarks>
    /// This setting controls how many requests a single client can make within the 
    /// configured time window before being rate limited.
    /// </remarks>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the time window duration in seconds.
    /// </summary>
    /// <value>The window duration in seconds. Defaults to 60 (1 minute).</value>
    /// <remarks>
    /// Defines the fixed window period during which <see cref="PermitLimit"/> requests are allowed.
    /// After the window expires, the counter resets.
    /// </remarks>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of requests that can be queued when the limit is reached.
    /// </summary>
    /// <value>The queue limit. Defaults to 0 (no queuing).</value>
    /// <remarks>
    /// When set to 0, requests exceeding the limit are immediately rejected.
    /// When greater than 0, excess requests are queued and processed when permits become available.
    /// </remarks>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// Gets or sets the inactivity timeout in minutes before cleaning up client limiters.
    /// </summary>
    /// <value>The timeout in minutes. Defaults to 5 minutes.</value>
    /// <remarks>
    /// Client rate limiters that haven't been accessed within this period are automatically
    /// disposed to free up memory resources.
    /// </remarks>
    public int InactivityTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to enable the circuit breaker for additional protection.
    /// </summary>
    /// <value><c>true</c> to enable circuit breaker; otherwise, <c>false</c>. Defaults to <c>false</c>.</value>
    /// <remarks>
    /// When enabled, the circuit breaker provides protection against cascading failures by
    /// temporarily blocking requests to a failing endpoint.
    /// </remarks>
    public bool EnableCircuitBreaker { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of failures before opening the circuit.
    /// </summary>
    /// <value>The failure threshold count. Defaults to 5.</value>
    /// <remarks>
    /// Only applicable when <see cref="EnableCircuitBreaker"/> is <c>true</c>.
    /// The circuit opens when this many failures occur within the sampling duration.
    /// </remarks>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration in seconds the circuit stays open before allowing test requests.
    /// </summary>
    /// <value>The break duration in seconds. Defaults to 30 seconds.</value>
    /// <remarks>
    /// Only applicable when <see cref="EnableCircuitBreaker"/> is <c>true</c>.
    /// After this duration, the circuit transitions to half-open state to test if the issue is resolved.
    /// </remarks>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
