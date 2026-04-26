# HoneyDrunk.Telemetry.Sink.Tempo

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Grafana Tempo trace sink for HoneyDrunk telemetry. Forwards OTLP trace data to Tempo for distributed trace storage and querying.

## Installation

```bash
dotnet add package HoneyDrunk.Telemetry.Sink.Tempo
```

## Configuration

```json
{
  "HoneyDrunk": {
    "Tempo": {
      "Endpoint": "http://tempo:4318",
      "TenantId": "my-tenant",
      "Username": "",
      "Password": ""
    }
  }
}
```

`Endpoint` should point at Tempo's **OTLP HTTP receiver** — typically port `4318` (use `4317` for OTLP/gRPC). Port `3200` is Tempo's *query frontend*; sending OTLP traces there will fail.

## Features

- OTLP protobuf and JSON forwarding to `/otlp/v1/traces`
- Multi-tenant support via `X-Scope-OrgID` header
- Basic authentication

## Related Projects

| Package | Description |
|---------|-------------|
| [HoneyDrunk.Telemetry.Abstractions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Sink interfaces |
| [HoneyDrunk.Telemetry.Sink.Loki](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Loki log sink |
| [HoneyDrunk.Telemetry.Sink.Mimir](https://github.com/HoneyDrunkStudios/HoneyDrunk.Pulse) | Grafana Mimir metrics sink |

## License

[MIT](https://opensource.org/licenses/MIT)

---

<p align="center"><strong>Built with 🍯 by HoneyDrunk Studios</strong></p>
<p align="center">
  <a href="https://github.com/HoneyDrunkStudios">GitHub</a>
</p>
