# HoneyDrunk.Telemetry.Abstractions

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Pure abstractions for HoneyDrunk telemetry sinks and instrumentation. Zero-dependency contracts safe for libraries and SDKs.

## What Is This?

This package defines the sink interfaces and telemetry models that all HoneyDrunk telemetry providers implement:

- **`ITraceSink`** — Forward OTLP trace data to a backend (Tempo, Azure Monitor)
- **`ILogSink`** — Forward OTLP log data to a backend (Loki, Azure Monitor)
- **`IMetricsSink`** — Forward OTLP metrics data to a backend (Mimir, Azure Monitor)
- **`IAnalyticsSink`** — Capture product analytics events (PostHog)
- **`IErrorSink`** — Route error events to an error tracker (Sentry)

### What This Package Does NOT Provide

- ❌ Concrete sink implementations (see `HoneyDrunk.Telemetry.Sink.*` packages)
- ❌ OpenTelemetry wiring (see `HoneyDrunk.Telemetry.OpenTelemetry`)
- ❌ OTLP parsing or HTTP endpoints (see Pulse.Collector)

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.Abstractions
```

## Usage

```csharp
// Implement a custom sink
public class MyTraceSink : ITraceSink
{
    public async Task ExportAsync(
        ReadOnlyMemory<byte> data,
        string contentType,
        CancellationToken cancellationToken)
    {
        // Forward trace data to your backend
    }
}
```

## Dependencies

| Package | Version |
|---------|---------|
| `HoneyDrunk.Kernel.Abstractions` | 0.4.0 |

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.OpenTelemetry](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | OTel integration for Grid Nodes |
| [HoneyDrunk.Telemetry.Sink.Tempo](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Tempo trace sink |
| [HoneyDrunk.Telemetry.Sink.Loki](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Loki log sink |
| [HoneyDrunk.Telemetry.Sink.Mimir](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Mimir metrics sink |
| [HoneyDrunk.Telemetry.Sink.PostHog](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | PostHog analytics sink |
| [HoneyDrunk.Telemetry.Sink.Sentry](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sentry error tracking sink |
| [HoneyDrunk.Telemetry.Sink.AzureMonitor](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Azure Monitor sink |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
