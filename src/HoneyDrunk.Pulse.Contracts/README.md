# HoneyDrunk.Pulse.Contracts

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Shared event contracts and DTOs for the HoneyDrunk.Pulse telemetry pipeline.

## What Is This?

This package contains the shared contracts that flow between Pulse components and downstream consumers:

- **`PulseIngested`** — Transport event published after each ingestion batch
- **`TelemetryEvent`** — Analytics event model for product telemetry
- **`ErrorEvent`** — Error event model for error tracking sinks
- **`ExtractedErrorSpan` / `ExtractedErrorLog`** — DTOs for routing errors from traces/logs to Sentry
- **`IngestionStatus`** — Enum indicating batch processing outcome

## Installation

```bash
dotnet add package HoneyDrunk.Pulse.Contracts
```

## Target Frameworks

| Framework | Supported |
|-----------|-----------|
| .NET 10.0 | ✅ |
| .NET 8.0 | ✅ |
| .NET Standard 2.0 | ✅ |

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces and telemetry models |
| [HoneyDrunk.Kernel.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) | Grid context and identity primitives |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
