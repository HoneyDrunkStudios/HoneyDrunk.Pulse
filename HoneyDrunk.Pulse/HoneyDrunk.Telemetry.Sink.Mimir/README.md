# HoneyDrunk.Telemetry.Sink.Mimir

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Grafana Mimir metrics sink for HoneyDrunk telemetry. Forwards OTLP metrics data to Mimir for long-term metrics storage and querying.

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.Sink.Mimir
```

## Configuration

```json
{
  "HoneyDrunk": {
    "Mimir": {
      "Endpoint": "http://mimir:9009",
      "TenantId": "my-tenant",
      "Username": "",
      "Password": ""
    }
  }
}
```

## Features

- OTLP protobuf and JSON forwarding to `/otlp/v1/metrics`
- Multi-tenant support via `X-Scope-OrgID` header
- Basic authentication

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces |
| [HoneyDrunk.Telemetry.Sink.Tempo](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Tempo trace sink |
| [HoneyDrunk.Telemetry.Sink.Loki](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Loki log sink |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
