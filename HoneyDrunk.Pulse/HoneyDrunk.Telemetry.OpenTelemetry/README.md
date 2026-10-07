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

## Default redaction and limits

`AddHoneyDrunkOpenTelemetry` registers standard OpenTelemetry span and log processors
before its OTLP exporters. They redact password, token, API key, authorization, cookie,
secret, connection-string, email, and phone fields without case/punctuation distinctions.
Common labeled credentials, authorization values, URL credentials, and email addresses
are also removed from free text. Nested dictionaries, lists, and JSON values are copied
and sanitized. The analytics HTTP emitter uses the same policy before transmission.

- Span display names, all tags (including duplicates), status descriptions, event names
  and tags, and link tags are sanitized through public .NET activity APIs
- Log bodies, attributes, and formatted messages are sanitized. When a structured value
  is removed, the already-formatted message is replaced because it may contain that value
  without its label
- Raw exception objects are removed. Sanitized exception type, message, and stack trace
  are retained as `exception.type`, `exception.message`, and `exception.stacktrace`;
  in-process errors also retain up to 128 sanitized method/file/line frames in
  `exception.stacktrace.frames` for native backend grouping. Raw exception data and
  inner-exception graphs are not retained
- Scope export is disabled by default: OpenTelemetry 1.19 does not expose a public API
  for replacing log scopes. Put necessary, non-sensitive correlation fields in structured
  log attributes. Re-enabling scopes bypasses this protection
- W3C trace IDs, span IDs, and parent relationships are unchanged. Business correlation
  and operation IDs remain separate attributes, never substituted for W3C IDs

`TelemetryRedactor` in `HoneyDrunk.Telemetry.OpenTelemetry.Redaction` is reusable for
collector ingress and other explicit boundaries. Inputs are limited to 16,384 text
characters, eight nested levels, and 1,024 inspected values per field/property collection.
Oversized, unsupported, or excessively nested values fail closed as `[REDACTED]`.
Text patterns use the non-backtracking regex engine with a timeout.

This is defense in depth, not complete data-loss prevention. Arbitrary unlabeled secrets,
unknown personal data, resource/instrumentation-scope metadata, metric labels, and baggage
propagated on outbound requests are not guaranteed to be sanitized. Never put sensitive
data in those paths. Processors/exporters registered earlier, separate providers, custom
sinks used directly, or later processors adding raw data are outside the default boundary.

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
