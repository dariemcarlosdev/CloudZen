using CloudZen.Api.Models;
using CloudZen.Api.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace CloudZen.Api.Services;

/// <summary>
/// Polly-based rate limiter service implementation with per-client rate limiting.
/// </summary>
/// <remarks>
/// <para>
/// Uses Polly's <see cref="ResiliencePipeline"/> for robust, composable resilience strategies
/// including fixed window rate limiting and optional circuit breaker protection.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Per-client fixed window rate limiting using <see cref="FixedWindowRateLimiter"/></description></item>
///   <item><description>Optional circuit breaker for cascading failure protection</description></item>
///   <item><description>Automatic cleanup of inactive client limiters to manage memory</description></item>
///   <item><description>Configurable via <see cref="IOptions{TOptions}"/> pattern with <see cref="RateLimitOptions"/></description></item>
///   <item><description>Comprehensive telemetry logging for monitoring and debugging</description></item>
/// </list>
/// </para>
/// <para>
/// The service maintains a separate rate limiter for each unique client/endpoint combination,
/// allowing fine-grained control over request rates.
/// </para>
/// </remarks>
/// <example>
/// Register in <c>Program.cs</c>:
/// <code>
/// builder.Services.Configure&lt;RateLimitOptions&gt;(
///     builder.Configuration.GetSection(RateLimitOptions.SectionName));
/// builder.Services.AddSingleton&lt;IRateLimiterService, PollyRateLimiterService&gt;();
/// </code>
/// </example>
public class PollyRateLimiterService : IRateLimiterService, IDisposable
{
    private readonly ILogger<PollyRateLimiterService> _logger;
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, ClientRateLimiter> _clientLimiters = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyRateLimiterService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="options">The rate limiting configuration options.</param>
    /// <remarks>
    /// The constructor starts a background timer for periodic cleanup of inactive client limiters.
    /// The cleanup interval is determined by <see cref="RateLimitOptions.InactivityTimeoutMinutes"/>.
    /// </remarks>
    public PollyRateLimiterService(
        ILogger<PollyRateLimiterService> logger,
        IOptions<RateLimitOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        _logger.LogInformation(
            "Initializing PollyRateLimiterService with PermitLimit={PermitLimit}, WindowSeconds={WindowSeconds}, CircuitBreaker={CircuitBreaker}",
            _options.PermitLimit,
            _options.WindowSeconds,
            _options.EnableCircuitBreaker);

        // Schedule periodic cleanup of inactive clients
        _cleanupTimer = new Timer(
            CleanupInactiveClients,
            null,
            TimeSpan.FromMinutes(_options.InactivityTimeoutMinutes),
            TimeSpan.FromMinutes(_options.InactivityTimeoutMinutes));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The method executes through a Polly resilience pipeline that may include:
    /// <list type="bullet">
    ///   <item><description>Rate limiter strategy (always enabled)</description></item>
    ///   <item><description>Circuit breaker strategy (if <see cref="RateLimitOptions.EnableCircuitBreaker"/> is <c>true</c>)</description></item>
    /// </list>
    /// </remarks>
    public async Task<RateLimitResult> TryAcquireAsync(string clientIdentifier, string endpoint)
    {
        var key = $"{clientIdentifier}:{endpoint}";

        var clientLimiter = _clientLimiters.GetOrAdd(key, _ => CreateClientLimiter(key));
        clientLimiter.LastAccessed = DateTime.UtcNow;

        try
        {
            // Execute through Polly resilience pipeline
            await clientLimiter.Pipeline.ExecuteAsync(
                static async _ => await ValueTask.CompletedTask,
                CancellationToken.None);

            var stats = clientLimiter.RateLimiter.GetStatistics();
            var remaining = (int)(stats?.CurrentAvailablePermits ?? 0);

            _logger.LogDebug(
                "Rate limit check passed for {ClientIdentifier} on {Endpoint}. Remaining: {Remaining}",
                clientIdentifier,
                endpoint,
                remaining);

            return RateLimitResult.Allowed(remaining);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Circuit breaker open for client {ClientIdentifier} on endpoint {Endpoint}",
                clientIdentifier,
                endpoint);

            return RateLimitResult.Limited(
                TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds),
                RateLimitRejectionReason.CircuitBreakerOpen);
        }
        catch (RateLimiterRejectedException ex)
        {
            var retryAfter = ex.RetryAfter ?? TimeSpan.FromSeconds(_options.WindowSeconds);

            _logger.LogWarning(
                "Rate limit exceeded for client {ClientIdentifier} on endpoint {Endpoint}. Retry after: {RetryAfter}s",
                clientIdentifier,
                endpoint,
                retryAfter.TotalSeconds);

            return RateLimitResult.Limited(retryAfter, RateLimitRejectionReason.RateLimitExceeded);
        }
    }

    /// <inheritdoc />
    public RateLimiterStatistics? GetStatistics(string clientIdentifier, string endpoint)
    {
        var key = $"{clientIdentifier}:{endpoint}";
        return _clientLimiters.TryGetValue(key, out var limiter)
            ? limiter.RateLimiter.GetStatistics()
            : null;
    }

    /// <summary>
    /// Creates a new client rate limiter with the configured resilience pipeline.
    /// </summary>
    /// <param name="key">The unique key identifying the client/endpoint combination.</param>
    /// <returns>A new <see cref="ClientRateLimiter"/> instance with configured strategies.</returns>
    private ClientRateLimiter CreateClientLimiter(string key)
    {
        _logger.LogDebug("Creating new rate limiter for key: {Key}", key);

        var rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = _options.PermitLimit,
            Window = TimeSpan.FromSeconds(_options.WindowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = _options.QueueLimit
        });

        // Build composable Polly resilience pipeline
        var pipelineBuilder = new ResiliencePipelineBuilder();

        // Add rate limiter (always enabled)
        pipelineBuilder.AddRateLimiter(new RateLimiterStrategyOptions
        {
            RateLimiter = args => rateLimiter.AcquireAsync(1, args.Context.CancellationToken),
            OnRejected = args =>
            {
                _logger.LogDebug("Request rejected by rate limiter for key: {Key}", key);
                return ValueTask.CompletedTask;
            }
        });

        // Optionally add circuit breaker for additional protection
        if (_options.EnableCircuitBreaker)
        {
            pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = _options.CircuitBreakerFailureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(_options.WindowSeconds),
                BreakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds),
                OnOpened = args =>
                {
                    _logger.LogWarning("Circuit breaker opened for key: {Key}", key);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed for key: {Key}", key);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker half-opened for key: {Key}", key);
                    return ValueTask.CompletedTask;
                }
            });
        }

        var pipeline = pipelineBuilder.Build();

        return new ClientRateLimiter
        {
            RateLimiter = rateLimiter,
            Pipeline = pipeline,
            LastAccessed = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Removes and disposes rate limiters for clients that have been inactive.
    /// </summary>
    /// <param name="state">Timer callback state (unused).</param>
    /// <remarks>
    /// This method is called periodically by the cleanup timer to free memory
    /// used by inactive client limiters.
    /// </remarks>
    private void CleanupInactiveClients(object? state)
    {
        if (_disposed) return;

        var cutoff = DateTime.UtcNow.AddMinutes(-_options.InactivityTimeoutMinutes);
        var keysToRemove = _clientLimiters
            .Where(kvp => kvp.Value.LastAccessed < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            if (_clientLimiters.TryRemove(key, out var limiter))
            {
                limiter.Dispose();
                _logger.LogDebug("Cleaned up inactive rate limiter for key: {Key}", key);
            }
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} inactive rate limiters. Active limiters: {Active}",
                keysToRemove.Count,
                _clientLimiters.Count);
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="PollyRateLimiterService"/>.
    /// </summary>
    /// <remarks>
    /// Disposes the cleanup timer and all client rate limiters.
    /// This method is safe to call multiple times.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cleanupTimer.Dispose();

        foreach (var limiter in _clientLimiters.Values)
        {
            limiter.Dispose();
        }
        _clientLimiters.Clear();

        _logger.LogInformation("PollyRateLimiterService disposed");
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Holds the rate limiter and resilience pipeline for a specific client/endpoint combination.
    /// </summary>
    /// <remarks>
    /// This internal class encapsulates the Polly rate limiter and resilience pipeline
    /// along with tracking information for cleanup purposes. Keep it sealed and simple to avoid unintended side effects. It is only used internally by the <see cref="PollyRateLimiterService"/> and is not exposed outside of this context. So here is the right place to add any additional properties or methods needed for managing the client's rate limiter without affecting the public API of the service. The <see cref="Dispose"/> method ensures that resources are properly released when a client limiter is removed or when the service is disposed.
    /// </remarks>
    private sealed class ClientRateLimiter : IDisposable
    {
        /// <summary>
        /// Gets the fixed window rate limiter for this client.
        /// </summary>
        public required FixedWindowRateLimiter RateLimiter { get; init; }

        /// <summary>
        /// Gets the Polly resilience pipeline containing rate limiting and optional circuit breaker.
        /// </summary>
        public required ResiliencePipeline Pipeline { get; init; }

        /// <summary>
        /// Gets or sets the timestamp of the last access to this limiter.
        /// </summary>
        /// <remarks>
        /// Used by the cleanup process to identify inactive limiters.
        /// </remarks>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        /// Disposes the underlying rate limiter.
        /// </summary>
        public void Dispose()
        {
            RateLimiter.Dispose();
        }
    }
}
