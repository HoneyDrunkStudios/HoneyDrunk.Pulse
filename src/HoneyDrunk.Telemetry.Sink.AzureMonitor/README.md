# HoneyDrunk.Telemetry.Sink.AzureMonitor

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Azure Monitor sink for HoneyDrunk telemetry. Exports traces, metrics, and logs to Application Insights using the Azure Monitor OpenTelemetry exporter.

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.Sink.AzureMonitor
```

## Configuration

```json
{
  "HoneyDrunk": {
    "AzureMonitor": {
      "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
    }
  }
}
```

## Features

- Implements `ITraceSink`, `ILogSink`, and `IMetricsSink` in a single sink
- OTLP data forwarding via Azure Monitor OpenTelemetry Exporter
- Connection string configuration for Application Insights

## Dependencies

| Package | Version |
|---------|---------|
| `Azure.Monitor.OpenTelemetry.Exporter` | 1.6.0 |
| `OpenTelemetry` | 1.15.0 |

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces |
| [HoneyDrunk.Telemetry.Sink.Tempo](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Tempo trace sink |
| [HoneyDrunk.Telemetry.Sink.Loki](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Loki log sink |
| [HoneyDrunk.Telemetry.Sink.Mimir](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Mimir metrics sink |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
