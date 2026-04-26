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

### gRPC Endpoints (Implemented)

OTLP gRPC is implemented via the standard OTLP service contracts:

| Service | Method | Description |
|---------|--------|-------------|
| `opentelemetry.proto.collector.trace.v1.TraceService` | `Export` | OTLP traces over gRPC |
| `opentelemetry.proto.collector.metrics.v1.MetricsService` | `Export` | OTLP metrics over gRPC |
| `opentelemetry.proto.collector.logs.v1.LogsService` | `Export` | OTLP logs over gRPC |

The host registers gRPC services on the same Kestrel listener as the HTTP endpoints (HTTP/2-enabled). Clients should target the standard OTLP gRPC port (commonly `4317`) when configured, or the host's listening port for local runs.

For deployment guidance and examples of pointing OTel SDK exporters at the collector, see [HoneyDrunk.Telemetry.OpenTelemetry README](../HoneyDrunk.Telemetry.OpenTelemetry/README.md).

## Health Endpoints

| Endpoint | Description |
|----------|-------------|
| `/health` | Liveness check |
| `/health/ready` | Readiness check (detailed) |
| `/health/live` | Liveness probe (Kubernetes / Container Apps) |

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
