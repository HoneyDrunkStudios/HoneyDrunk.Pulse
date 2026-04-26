# HoneyDrunk.Telemetry.Sink.Sentry

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Sentry error tracking sink for HoneyDrunk telemetry. Routes error events, exceptions, and error spans to Sentry with enriched Grid context.

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.Sink.Sentry
```

## Configuration

```json
{
  "HoneyDrunk": {
    "Sentry": {
      "DsnSecretName": "Sentry--Dsn",
      "Environment": "production"
    }
  }
}
```

The DSN value is resolved from `ISecretStore` and the SDK is reinitialized when the current Vault value/version changes.

## Features

- Error event capture with severity mapping
- Exception details forwarding (type, message, stacktrace)
- Tag and extra data propagation
- Grid context enrichment (CorrelationId, NodeId, Environment)
- Depends on Sentry SDK 6.4.1

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces |
| [HoneyDrunk.Telemetry.Sink.PostHog](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | PostHog analytics sink |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
