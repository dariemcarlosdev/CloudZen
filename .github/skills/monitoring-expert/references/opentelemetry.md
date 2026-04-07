# OpenTelemetry Reference

> **Load when:** Implementing distributed tracing, OTLP export, or custom spans.

## OpenTelemetry Setup for .NET

### Full Configuration

```csharp
// Program.cs — Complete OpenTelemetry setup
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "MyApp",
            serviceVersion: typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext =>
                !httpContext.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
            options.FilterHttpRequestMessage = request =>
                request.RequestUri?.Host != "localhost"; // skip local calls
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true; // include SQL in spans
        })
        .AddSource("MyApp.*") // custom activity sources
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
            options.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MyApp.*") // custom meters
        .AddPrometheusExporter());
```

### Required Packages

```xml
<ItemGroup>
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.*" />
</ItemGroup>
```

## Custom Activity Sources (Spans)

### Business Operation Tracing

```csharp
public sealed class EscrowActivitySource
{
    public static readonly ActivitySource Source = new("MyApp.Orders", "1.0.0");

    public static Activity? StartCreateEscrow(string buyerId, string sellerId, decimal amount)
    {
        var activity = Source.StartActivity("order.create", ActivityKind.Internal);
        activity?.SetTag("order.buyer_id", buyerId);
        activity?.SetTag("order.seller_id", sellerId);
        activity?.SetTag("order.amount", amount);
        activity?.SetTag("order.currency", "USD");
        return activity;
    }

    public static Activity? StartProcessPayment(string orderId, string provider)
    {
        var activity = Source.StartActivity("payment.process", ActivityKind.Client);
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("payment.provider", provider);
        return activity;
    }
}
```

### Usage in MediatR Handlers

```csharp
public sealed class CreateEscrowHandler : IRequestHandler<CreateOrderCommand, EscrowResult>
{
    private readonly IEscrowRepository _repository;

    public async Task<EscrowResult> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        using var activity = EscrowActivitySource.StartCreateEscrow(
            cmd.BuyerId, cmd.SellerId, cmd.Amount);

        try
        {
            var order = Escrow.Create(cmd.BuyerId, cmd.SellerId, cmd.Amount);
            await _repository.AddAsync(order, ct);

            activity?.SetTag("order.id", order.Id.Value);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new EscrowResult(order.Id);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Context Propagation

### W3C TraceContext (Default)

OpenTelemetry uses W3C TraceContext by default. The `traceparent` header propagates across HTTP boundaries:

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
              │  │                                │                  │
              │  trace-id (128-bit)               span-id (64-bit)  sampled
              version
```

### Baggage for Cross-Service Context

```csharp
// Add business context that propagates across all services
Baggage.SetBaggage("order.id", orderId);
Baggage.SetBaggage("tenant.id", tenantId);

// Read baggage in downstream services
var orderId = Baggage.GetBaggage("order.id");
```

### MediatR Tracing Behavior

```csharp
public sealed class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("MyApp.MediatR");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity(
            $"MediatR.{typeof(TRequest).Name}",
            ActivityKind.Internal);

        activity?.SetTag("mediatr.request_type", typeof(TRequest).FullName);

        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## OTLP Collector Configuration

```yaml
# otel-collector-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4317"
      http:
        endpoint: "0.0.0.0:4318"

processors:
  batch:
    timeout: 5s
    send_batch_size: 1024

exporters:
  jaeger:
    endpoint: "jaeger:14250"
    tls:
      insecure: true
  prometheus:
    endpoint: "0.0.0.0:8889"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

## Span Naming Conventions

| Category | Pattern | Example |
|---|---|---|
| HTTP Server | `{HTTP_METHOD} {route}` | `POST /api/orders` |
| HTTP Client | `HTTP {method}` | `HTTP POST` |
| Database | `{operation} {table}` | `SELECT orders` |
| Message Queue | `{queue} {operation}` | `order-events publish` |
| Custom Business | `{domain}.{operation}` | `order.create` |
