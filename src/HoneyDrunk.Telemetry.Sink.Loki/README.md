# HoneyDrunk.Telemetry.Sink.Loki

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Grafana Loki log sink for HoneyDrunk telemetry. Forwards OTLP log data to Loki with configurable log level filtering and multi-tenant support.

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.Sink.Loki
```

## Configuration

```json
{
  "HoneyDrunk": {
    "Loki": {
      "Endpoint": "http://loki:3100",
      "TenantId": "my-tenant",
      "MinimumLogLevel": "Information",
      "Username": "",
      "Password": ""
    }
  }
}
```

## Features

- OTLP protobuf and JSON forwarding to `/otlp/v1/logs`
- Configurable minimum log level filtering
- Multi-tenant support via `X-Scope-OrgID` header
- Basic authentication

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces |
| [HoneyDrunk.Telemetry.Sink.Tempo](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Tempo trace sink |
| [HoneyDrunk.Telemetry.Sink.Mimir](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Mimir metrics sink |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
