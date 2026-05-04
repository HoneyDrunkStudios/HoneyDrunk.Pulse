# HoneyDrunk.Pulse

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **The Observability Engine for the Hive** — Telemetry abstractions, OpenTelemetry integration, multi-backend sink pipeline, and the Pulse Collector OTLP receiver that powers observability across every HoneyDrunk Grid Node.

**Signal Quote:** *"See everything. Miss nothing."*

## 📦 What Is This?

HoneyDrunk.Pulse is the observability pillar of the HoneyDrunk Grid. It provides:

- ✅ **Telemetry Abstractions** — Sink interfaces (`ITraceSink`, `ILogSink`, `IMetricsSink`, `IAnalyticsSink`, `IErrorSink`) and event models for any telemetry backend
- ✅ **OpenTelemetry Integration** — Preconfigured tracing, metrics, and logging pipelines with Grid context enrichment
- ✅ **Multi-Backend Sink Pipeline** — Fan-out to Grafana (Tempo, Loki, Mimir), Sentry, PostHog, and Azure Monitor simultaneously
- ✅ **Pulse Collector** — OTLP receiver (HTTP + gRPC) that ingests, enriches, and routes telemetry with per-sink failure isolation
- ✅ **Shared Contracts** — `PulseIngested` Transport events for downstream consumers

### What Pulse Is NOT

- ❌ **Not a replacement for OpenTelemetry** — Pulse builds on top of OTel, not instead of it
- ❌ **Not a storage engine** — Pulse routes to backends (Tempo, Loki, Mimir), it doesn't store telemetry
- ❌ **Not a dashboarding tool** — Use Grafana, Azure Monitor, or Sentry for visualization

## 🚀 Quick Start

### For Grid Nodes (Emit Telemetry)

Add OpenTelemetry instrumentation to any Node:

```bash
dotnet add package HoneyDrunk.Telemetry.OpenTelemetry
```

```csharp
builder.Services.AddHoneyDrunkOpenTelemetry(builder.Configuration);
```

```json
{
  "HoneyDrunk": {
    "OpenTelemetry": {
      "ServiceName": "my-node",
      "OtlpEndpoint": "http://pulse-collector:4318"
    }
  }
}
```

### For Custom Sinks (Consume Telemetry)

Implement the abstractions:

```bash
dotnet add package HoneyDrunk.Telemetry.Abstractions
```

```csharp
public class MyCustomSink : ITraceSink
{
    public async Task ExportAsync(
        ReadOnlyMemory<byte> data,
        string contentType,
        CancellationToken cancellationToken)
    {
        // Forward OTLP trace data to your backend
    }
}
```

## 🎯 Key Features

### 📡 Multi-Protocol OTLP Ingestion

The Pulse Collector accepts telemetry via:

| Protocol | Traces | Metrics | Logs |
|----------|--------|---------|------|
| HTTP (protobuf) | `/v1/traces` | `/v1/metrics` | `/v1/logs` |
| HTTP (JSON) | `/v1/traces` | `/v1/metrics` | `/v1/logs` |
| gRPC | `TraceService/Export` | `MetricsService/Export` | `LogsService/Export` |

### 🔀 Multi-Backend Sink Pipeline

Route telemetry to multiple backends simultaneously with per-sink failure isolation:

```
OTLP In ──► Enrichment ──► ┌─ Tempo (traces)
                           ├─ Loki (logs)
                           ├─ Mimir (metrics)
                           ├─ Sentry (errors)
                           ├─ PostHog (analytics)
                           └─ Azure Monitor (all signals)
```

Each sink operates independently — a Tempo outage won't affect Loki or Sentry delivery.

### 🏷️ Grid Context Enrichment

The `TelemetryEnricher` automatically stamps all telemetry with:

- `hive.node.id` / `hive.node.name` — Source Node identity
- `hive.studio.id` — Studio/organization context
- `hive.environment` — Deployment environment
- `hive.correlation.id` — Distributed correlation
- `honeydrunk.tenant_id` — ADR-0026 tenant context for bounded, low-cardinality reporting

Tenant discipline: `tenant_id` is safe as a Pulse metric tag only while v1 paying tenants remain in the tens; continued use is bounded by Notify Cloud's cardinality kill criteria, and user/session/request identifiers must not be placed in this dimension.

### 📤 Transport Event Publishing

After each ingestion batch, a `PulseIngested` event is published to Transport (InMemory or Azure Service Bus) for downstream consumers. Transport failures are isolated and never affect the HTTP response.

## 📖 Documentation

Detailed documentation is available in the `docs/` folder:

- [Architecture](HoneyDrunk.Pulse/docs/examples/Architecture.md)
- [Abstractions](HoneyDrunk.Pulse/docs/examples/Abstractions.md)
- [Configuration](HoneyDrunk.Pulse/docs/examples/Configuration.md)
- [Dependency Injection](HoneyDrunk.Pulse/docs/examples/DependencyInjection.md)
- [Middleware](HoneyDrunk.Pulse/docs/examples/Middleware.md)

## 🏗️ Project Structure

```
HoneyDrunk.Pulse/
├── HoneyDrunk.Pulse.Contracts/             # Shared event contracts (multi-target)
├── HoneyDrunk.Telemetry.Abstractions/      # Sink interfaces and telemetry models
├── HoneyDrunk.Telemetry.OpenTelemetry/     # OTel wiring for Grid Nodes
├── HoneyDrunk.Telemetry.Sink.Tempo/        # Grafana Tempo trace sink
├── HoneyDrunk.Telemetry.Sink.Loki/         # Grafana Loki log sink
├── HoneyDrunk.Telemetry.Sink.Mimir/        # Grafana Mimir metrics sink
├── HoneyDrunk.Telemetry.Sink.PostHog/      # PostHog analytics sink
├── HoneyDrunk.Telemetry.Sink.Sentry/       # Sentry error tracking sink
├── HoneyDrunk.Telemetry.Sink.AzureMonitor/ # Azure Monitor all-signals sink
├── Pulse.Collector/                         # OTLP receiver service (HTTP + gRPC)
├── HoneyDrunk.Pulse.Sample.Api/            # Sample web API with OTel
├── HoneyDrunk.Pulse.Sample.Worker/         # Sample worker with OTel
└── Pulse.Tests/                             # xUnit test suite
```

## 🆕 What's New in v0.1.0

- Initial release of all 9 packages
- OTLP HTTP + gRPC ingestion endpoints
- Multi-sink fan-out pipeline with failure isolation
- Grid context enrichment via `TelemetryEnricher`
- Transport event publishing with isolated error handling
- PostHog 429 rate-limit retry with `Retry-After` support
- 143 unit tests across 13 test classes

## 📦 Packages

| Package | Version | Description |
|---------|---------|-------------|
| `HoneyDrunk.Pulse.Contracts` | 0.1.0 | Shared event contracts (netstandard2.0 + net8.0 + net10.0) |
| `HoneyDrunk.Telemetry.Abstractions` | 0.1.0 | Sink interfaces and telemetry models |
| `HoneyDrunk.Telemetry.OpenTelemetry` | 0.1.0 | OTel wiring for Grid Nodes |
| `HoneyDrunk.Telemetry.Sink.Tempo` | 0.1.0 | Grafana Tempo trace sink |
| `HoneyDrunk.Telemetry.Sink.Loki` | 0.1.0 | Grafana Loki log sink |
| `HoneyDrunk.Telemetry.Sink.Mimir` | 0.1.0 | Grafana Mimir metrics sink |
| `HoneyDrunk.Telemetry.Sink.PostHog` | 0.1.0 | PostHog analytics sink |
| `HoneyDrunk.Telemetry.Sink.Sentry` | 0.1.0 | Sentry error tracking sink |
| `HoneyDrunk.Telemetry.Sink.AzureMonitor` | 0.1.0 | Azure Monitor sink |

## 🔗 Related Projects

| Project | Description |
|---------|-------------|
| [HoneyDrunk.Kernel](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) | Grid context, lifecycle, and identity primitives |
| [HoneyDrunk.Transport](https://github.com/HoneyDrunkStudios/HoneyDrunk.Transport) | Messaging and outbox infrastructure |
| [HoneyDrunk.Vault](https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault) | Secrets management |
| [HoneyDrunk.Auth](https://github.com/HoneyDrunkStudios/HoneyDrunk.Auth) | Authentication and authorization |
| [HoneyDrunk.Web.Rest](https://github.com/HoneyDrunkStudios/HoneyDrunk.Web.Rest) | REST API conventions |

## 📄 License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>