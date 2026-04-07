# Prometheus Metrics Reference

> **Load when:** Implementing counters, histograms, gauges, or the .NET metrics API with Prometheus.

## .NET Metrics API + Prometheus

### Setup with prometheus-net

```csharp
// Program.cs — Add Prometheus metrics endpoint
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();
app.MapPrometheusScrapingEndpoint(); // Exposes /metrics
```

### Alternative: prometheus-net Direct

```xml
<PackageReference Include="prometheus-net.AspNetCore" Version="8.*" />
```

```csharp
app.UseHttpMetrics();        // Auto HTTP metrics
app.MapMetrics();            // /metrics endpoint
```

## Metric Types

### Counter — Monotonically Increasing Values

Use for: total requests, errors, events processed.

```csharp
public sealed class EscrowMetrics
{
    private readonly Counter<long> _ordersCreated;
    private readonly Counter<long> _ordersFailed;

    public EscrowMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MyApp.Orders");
        _ordersCreated = meter.CreateCounter<long>(
            "order.created.total",
            unit: "orders",
            description: "Total number of orders created");
        _ordersFailed = meter.CreateCounter<long>(
            "order.failed.total",
            unit: "orders",
            description: "Total number of failed order operations");
    }

    public void RecordCreated(string orderType)
        => _ordersCreated.Add(1, new KeyValuePair<string, object?>("type", orderType));

    public void RecordFailed(string reason)
        => _ordersFailed.Add(1, new KeyValuePair<string, object?>("reason", reason));
}
```

### Histogram — Distribution of Values

Use for: request latency, response sizes, processing durations.

```csharp
public sealed class PaymentMetrics
{
    private readonly Histogram<double> _processingDuration;
    private readonly Histogram<double> _paymentAmount;

    public PaymentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MyApp.Payments");

        _processingDuration = meter.CreateHistogram<double>(
            "payment.processing.duration",
            unit: "ms",
            description: "Payment processing duration in milliseconds");

        _paymentAmount = meter.CreateHistogram<double>(
            "payment.amount",
            unit: "USD",
            description: "Payment amounts processed");
    }

    public void RecordDuration(double ms, string provider)
        => _processingDuration.Record(ms, new KeyValuePair<string, object?>("provider", provider));

    public void RecordAmount(decimal amount)
        => _paymentAmount.Record((double)amount);
}
```

### Gauge — Point-in-Time Values

Use for: active connections, queue depth, cache size.

```csharp
public sealed class SystemMetrics
{
    private readonly ObservableGauge<int> _activeCircuits;
    private readonly ObservableGauge<long> _queueDepth;

    public SystemMetrics(IMeterFactory meterFactory, ICircuitTracker tracker, IQueueMonitor queue)
    {
        var meter = meterFactory.Create("MyApp.System");

        _activeCircuits = meter.CreateObservableGauge(
            "blazor.circuits.active",
            () => tracker.ActiveCount,
            unit: "circuits",
            description: "Number of active Blazor Server circuits");

        _queueDepth = meter.CreateObservableGauge(
            "order.queue.depth",
            () => queue.PendingCount,
            unit: "items",
            description: "Number of pending order operations in queue");
    }
}
```

## Naming Conventions

Follow OpenTelemetry semantic conventions:

```
{namespace}.{entity}.{action}[.{suffix}]

Examples:
  order.created.total          — Counter
  order.processing.duration    — Histogram
  payment.amount                — Histogram
  blazor.circuits.active        — Gauge
  http.server.request.duration  — Histogram (built-in)
```

**Label Guidelines:**
- Keep cardinality low (< 100 unique values per label)
- Good labels: `status`, `type`, `method`, `endpoint`
- Bad labels: `user_id`, `order_id`, `request_id` (high cardinality)

## Prometheus Scrape Configuration

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'myapp'
    scrape_interval: 15s
    metrics_path: '/metrics'
    static_configs:
      - targets: ['myapp:8080']
        labels:
          environment: 'production'
          service: 'my-api'

  - job_name: 'myapp-blazor'
    scrape_interval: 15s
    metrics_path: '/metrics'
    static_configs:
      - targets: ['myapp-web:8080']
        labels:
          environment: 'production'
          service: 'order-web'
```

## PromQL Query Examples

```promql
# Request rate (requests per second)
rate(http_server_request_duration_seconds_count{service="my-api"}[5m])

# Error rate percentage
100 * rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])
/ rate(http_server_request_duration_seconds_count[5m])

# P99 latency
histogram_quantile(0.99, rate(http_server_request_duration_seconds_bucket{service="my-api"}[5m]))

# Escrows created per minute
rate(order_created_total[1m]) * 60

# Active Blazor circuits
blazor_circuits_active{environment="production"}
```

## DI Registration Pattern

```csharp
// Register all metrics as singletons
public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddEscrowMetrics(this IServiceCollection services)
    {
        services.AddSingleton<EscrowMetrics>();
        services.AddSingleton<PaymentMetrics>();
        services.AddSingleton<SystemMetrics>();
        return services;
    }
}

// Usage in a MediatR handler
public sealed class CreateEscrowHandler(
    IEscrowRepository repository,
    EscrowMetrics metrics) : IRequestHandler<CreateOrderCommand, EscrowResult>
{
    public async Task<EscrowResult> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var order = Escrow.Create(request.BuyerId, request.SellerId, request.Amount);
            await repository.AddAsync(order, ct);
            metrics.RecordCreated(request.Type);
            return new EscrowResult(order.Id);
        }
        catch (Exception)
        {
            metrics.RecordFailed("creation_error");
            throw;
        }
        finally
        {
            // Always record duration, even on failure
            metrics.RecordDuration(sw.Elapsed.TotalMilliseconds, "create");
        }
    }
}
```
