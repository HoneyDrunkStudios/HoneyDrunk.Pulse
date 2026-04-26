# Pulse.Collector

The central telemetry ingestion service for HoneyDrunk.OS.

## Overview

Pulse.Collector receives OTLP telemetry data and routes it to:
- **Sentry** for error tracking
- **PostHog** for product analytics
- **HoneyDrunk.Transport** for internal Grid events (`Pulse.Ingested`)

## OTLP Endpoints

### HTTP Endpoints (Implemented)

| Endpoint | Method | Content-Type | Description |
|----------|--------|--------------|-------------|
| `/otlp/v1/traces` | POST | `application/json`, `application/x-protobuf` | OTLP traces |
| `/otlp/v1/metrics` | POST | `application/json`, `application/x-protobuf` | OTLP metrics |
| `/otlp/v1/logs` | POST | `application/json`, `application/x-protobuf` | OTLP logs |
| `/otlp/v1/analytics` | POST | `application/json` | Custom analytics events |
| `/otlp/v1/errors` | POST | `application/json` | Error reports |

### gRPC Endpoints (Not Yet Implemented)

The OTLP specification supports gRPC transport, but this is **not yet implemented**. The current implementation only supports HTTP/JSON and HTTP/protobuf.

To implement gRPC OTLP support, the following would be required:
1. Generate C# types from [OTLP proto definitions](https://github.com/open-telemetry/opentelemetry-proto)
2. Implement `TraceService.Export`, `MetricsService.Export`, and `LogsService.Export` gRPC services
3. Register gRPC services in the ASP.NET Core pipeline

For most use cases, HTTP OTLP is sufficient and simpler to deploy.

## Health Endpoints

| Endpoint | Description |
|----------|-------------|
| `/health` | Liveness check |
| `/health/ready` | Readiness check (detailed) |

## Configuration

### Required Secrets (via Vault)

| Key | Description |
|-----|-------------|
| `PostHog--ApiKey` | PostHog API key for analytics |
| `Sentry--Dsn` | Sentry DSN for error tracking |
| `Loki--BasicAuth` | Optional Loki authorization header or Basic auth value |
| `Tempo--BasicAuth` | Optional Tempo authorization header or Basic auth value |
| `Mimir--BasicAuth` | Optional Mimir authorization header or Basic auth value |

Use `Loki--Username` + `Loki--Password`, `Tempo--Username` + `Tempo--Password`, or `Mimir--Username` + `Mimir--Password` when separate Basic auth credentials are preferred.

### Application Settings

```json
{
  "HoneyDrunk:Pulse:Collector": {
    "ServiceName": "Pulse.Collector",
    "StudioId": "honeydrunk",
    "Environment": "development",
    "EnableTransportPublishing": true,
    "EnablePostHogSink": true,
    "EnableSentrySink": true,
    "RequireOtlpAuthentication": false,
    "MaxBatchSize": 1000,
    "ProcessingTimeoutSeconds": 30
  },
  "HoneyDrunk:PostHog": {
    "ApiKeySecretName": "PostHog--ApiKey",
    "Host": "https://app.posthog.com"
  },
  "HoneyDrunk:Sentry": {
    "DsnSecretName": "Sentry--Dsn",
    "Environment": "production"
  }
}
```

## Telemetry Processing

### Enrichment

All telemetry is enriched with:
- `service.name` (defaulted to "unknown-service" if missing)
- `honeydrunk.environment` from collector configuration
- `honeydrunk.correlation_id` and `honeydrunk.operation_id` from Kernel context (when available)
- `pulse.ingested_at` timestamp

### JSON Parsing (Accurate)

For JSON payloads, the parser navigates the OTLP structure to count actual items:
- Traces: `resourceSpans → scopeSpans → spans`
- Metrics: `resourceMetrics → scopeMetrics → metrics`
- Logs: `resourceLogs → scopeLogs → logRecords`

### Protobuf Parsing (Heuristic)

For protobuf payloads, counts are estimated based on payload size:
- Spans: ~300 bytes per span
- Metrics: ~100 bytes per metric
- Log records: ~150 bytes per record

## Running Locally

```bash
cd Pulse.Collector
dotnet run
```

The collector will start on `http://localhost:5000` by default.

## Docker

```bash
docker build -t pulse-collector .
docker run -p 5000:8080 pulse-collector
```
