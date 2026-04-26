# HoneyDrunk.Telemetry.Sink.PostHog

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> PostHog analytics sink for HoneyDrunk telemetry. Captures product analytics events with batching, retry logic, and rate-limit handling.

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.Sink.PostHog
```

## Configuration

```json
{
  "HoneyDrunk": {
    "PostHog": {
      "ApiKeySecretName": "PostHog--ApiKey",
      "Host": "https://app.posthog.com",
      "BatchSize": 100,
      "FlushIntervalMs": 30000,
      "MaxRetries": 3
    }
  }
}
```

The API key value is resolved from `ISecretStore` on flush so Event Grid cache invalidation and secret rotation take effect without a process restart.

## Features

- HTTP-based batch event capture via `/batch` endpoint
- Configurable batch size and flush interval
- Event mapping from `TelemetryEvent` to PostHog capture format
- Property filtering via approved/excluded key lists
- Retry with exponential backoff for transient failures
- HTTP 429 rate-limit handling with `Retry-After` header support

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces |
| [HoneyDrunk.Telemetry.Sink.Sentry](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sentry error tracking sink |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
