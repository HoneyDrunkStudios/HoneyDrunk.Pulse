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

### Ingestion Authentication

**Deployment migration:** outside the ASP.NET Core `Development` environment, the collector
now refuses to start with anonymous ingestion unless it has been deliberately opted into.
The collector's `Environment` telemetry label does not control this check. Existing
deployments must configure one of the following before adopting this version.

#### JWT bearer authentication (recommended)

Set these keys in the existing configuration source (for example App Configuration or
environment variables, replacing `:` with `__`):

```json
{
  "HoneyDrunk:Pulse:Collector:RequireOtlpAuthentication": true,
  "HoneyDrunk:Pulse:Collector:OtlpAuthenticationAuthority": "https://identity.example.com",
  "HoneyDrunk:Pulse:Collector:OtlpAuthenticationAudience": "pulse-ingestion"
}
```

The authority must expose standard OpenID Connect discovery and signing-key metadata over
HTTPS. These example values are placeholders; use your identity provider and a dedicated
collector API audience. This change does not provision an identity provider, client
registration, permissions, or credentials. Missing or invalid authority/audience settings
fail startup even in Development and even when the anonymous opt-out is enabled.

All five HTTP ingestion endpoints and all three gRPC `Export` services require the same
ASP.NET Core JWT bearer policy. Clients must obtain an access token for the configured
audience and send `Authorization: Bearer <access-token>` (gRPC metadata: `authorization`).
The caller or its gateway must renew short-lived tokens before they expire. Use HTTPS
for client connections, including when TLS terminates at a trusted ingress.

For standard OpenTelemetry exporters, configure authorization through
`OTEL_EXPORTER_OTLP_HEADERS` or the signal-specific exporter headers. The custom
analytics emitter uses a separate named `HttpClient` (`PulseAnalyticsEmitterOptions.HttpClientName`);
configure that client with a token-acquisition `DelegatingHandler` through `AddHttpClient`
and `AddHttpMessageHandler`. It does not read OTLP exporter headers or acquire tokens itself.
Custom HTTP error reporters must also supply their own bearer header. Never persist token
values in source or logs; host applications own acquisition and rotation.

The collector validates signature, issuer, audience, and expiration with 30 seconds of
clock tolerance. Missing, invalid, or expired tokens are rejected with HTTP 401 (gRPC
clients observe an unauthenticated failure). Discovery/key retrieval failures do not
fall back to anonymous access; previously retrieved valid signing metadata may be reused
by the standard handler. Inbound bearer tokens are not forwarded as sink credentials.

Only authorize trusted telemetry producers to obtain this dedicated audience. A valid
token grants ingestion access; this policy does not additionally enforce application
roles, OAuth scopes, or a relationship between the token and a submitted tenant ID.
Payload tenant and source fields remain caller-supplied telemetry, not an authorization
boundary. Health probes (`/health`, `/health/live`, `/health/ready`) remain anonymous.
The Vault invalidation webhook retains its own validation and is outside this policy.
Outbound sink secrets continue to resolve through Vault; they are not ingestion tokens.

#### Authenticated gateway with an explicit anonymous collector opt-out

If a separately managed gateway already authenticates every HTTP and gRPC ingestion
request, an operator may deliberately set both:

```json
{
  "HoneyDrunk:Pulse:Collector:RequireOtlpAuthentication": false,
  "HoneyDrunk:Pulse:Collector:AllowUnauthenticatedOtlpInNonDevelopment": true
}
```

This disables authentication inside the collector. It neither configures nor verifies
gateway authentication. Block direct network access to the collector and verify the
gateway covers all eight ingestion routes before opting out. Merely running on a
private network, using Container Apps, or sending identity headers does not establish
authentication. Do not use this opt-out for a publicly reachable collector.

Development retains anonymous ingestion by default. Restart the collector after changing
these settings. Keep token acquisition credentials out of committed configuration and logs.

### Application Settings

The following example assumes the host is running in `Development`; use one of the
ingestion authentication configurations above for other environments.

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

Using the project's `launchSettings.json`, the collector starts on `http://localhost:5077` by default. (Plain `dotnet run` outside Visual Studio / `dotnet watch` falls back to ASP.NET Core's `http://localhost:5000` default unless `ASPNETCORE_URLS` is set.)

## Docker

```bash
docker build -t pulse-collector .
docker run -p 5000:8080 pulse-collector
```
