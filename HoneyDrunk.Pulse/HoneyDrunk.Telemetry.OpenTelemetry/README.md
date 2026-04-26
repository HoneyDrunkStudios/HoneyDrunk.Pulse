# HoneyDrunk.Telemetry.OpenTelemetry

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> OpenTelemetry integration for HoneyDrunk Grid Nodes. Preconfigured tracing, metrics, and logging pipelines with OTLP export and Grid context enrichment.

## What Is This?

This package wires up a complete OpenTelemetry stack for any HoneyDrunk Grid Node:

- **Tracing** — ASP.NET Core + HTTP client instrumentation with OTLP export
- **Metrics** — Runtime + process instrumentation with OTLP export
- **Logging** — Structured log export via OTLP
- **Grid Enrichment** — Automatic NodeId, StudioId, and Environment attributes on all spans

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.OpenTelemetry
```

## Quick Start

```csharp
builder.Services.AddHoneyDrunkOpenTelemetry(builder.Configuration);
```

## Configuration

```json
{
  "HoneyDrunk": {
    "OpenTelemetry": {
      "ServiceName": "my-node",
      "OtlpEndpoint": "http://localhost:4317"
    }
  }
}
```

## Dependencies

| Package | Version |
|---------|---------|
| `HoneyDrunk.Kernel.Abstractions` | 0.4.0 |
| `OpenTelemetry` | 1.15.0 |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.15.0 |
| `OpenTelemetry.Extensions.Hosting` | 1.15.0 |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.15.0 |
| `OpenTelemetry.Instrumentation.Http` | 1.15.0 |
| `OpenTelemetry.Instrumentation.Runtime` | 1.15.0 |

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces and telemetry models |
| [HoneyDrunk.Kernel](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) | Grid context and lifecycle runtime |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
