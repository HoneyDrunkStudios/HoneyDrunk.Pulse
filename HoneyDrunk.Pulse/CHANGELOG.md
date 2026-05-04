# HoneyDrunk.Pulse - Repository Changelog

All notable changes to the HoneyDrunk.Pulse repository will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

**Note:** See individual package CHANGELOGs for detailed per-package changes:

- [HoneyDrunk.Pulse.Contracts](HoneyDrunk.Pulse.Contracts/CHANGELOG.md)
- [HoneyDrunk.Telemetry.Abstractions](HoneyDrunk.Telemetry.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Telemetry.OpenTelemetry](HoneyDrunk.Telemetry.OpenTelemetry/CHANGELOG.md)
- [HoneyDrunk.Telemetry.Sink.Loki](HoneyDrunk.Telemetry.Sink.Loki/CHANGELOG.md)
- [HoneyDrunk.Telemetry.Sink.Tempo](HoneyDrunk.Telemetry.Sink.Tempo/CHANGELOG.md)
- [HoneyDrunk.Telemetry.Sink.Mimir](HoneyDrunk.Telemetry.Sink.Mimir/CHANGELOG.md)
- [HoneyDrunk.Telemetry.Sink.PostHog](HoneyDrunk.Telemetry.Sink.PostHog/CHANGELOG.md)
- [HoneyDrunk.Telemetry.Sink.Sentry](HoneyDrunk.Telemetry.Sink.Sentry/CHANGELOG.md)
- [HoneyDrunk.Telemetry.Sink.AzureMonitor](HoneyDrunk.Telemetry.Sink.AzureMonitor/CHANGELOG.md)

## [0.2.0] - 2026-05-04

### Changed

- Migrated Pulse.Collector bootstrap to env-driven Azure Key Vault and Azure App Configuration discovery using the `pulse` App Configuration label.
- Registered the Vault Event Grid cache invalidation webhook at `/internal/vault/invalidate`.
- Moved PostHog, Sentry, Loki, Tempo, and Mimir sink credentials behind `ISecretStore` with provider-grouped secret names.
- Updated deploy workflow authentication to Azure OIDC and removed client-secret/registry-password inputs.

### Added

- Added ADR-0026 tenant context propagation for Pulse collector ingress, analytics events, error reports, and low-cardinality collector telemetry tags.
- Added a PostHog credential rotation canary covering per-flush Vault secret resolution.

## [0.1.0] - 2026-02-21

### 🎯 Initial Release

First public release of the HoneyDrunk.Pulse telemetry ecosystem.

#### Packages

| Package | Version | Description |
|---------|---------|-------------|
| `HoneyDrunk.Pulse.Contracts` | 0.1.0 | Shared event contracts (multi-target: netstandard2.0, net8.0, net10.0) |
| `HoneyDrunk.Telemetry.Abstractions` | 0.1.0 | Sink interfaces and telemetry models |
| `HoneyDrunk.Telemetry.OpenTelemetry` | 0.1.0 | OTel wiring for Grid Nodes |
| `HoneyDrunk.Telemetry.Sink.Loki` | 0.1.0 | Grafana Loki log sink |
| `HoneyDrunk.Telemetry.Sink.Tempo` | 0.1.0 | Grafana Tempo trace sink |
| `HoneyDrunk.Telemetry.Sink.Mimir` | 0.1.0 | Grafana Mimir metrics sink |
| `HoneyDrunk.Telemetry.Sink.PostHog` | 0.1.0 | PostHog analytics sink |
| `HoneyDrunk.Telemetry.Sink.Sentry` | 0.1.0 | Sentry error tracking sink |
| `HoneyDrunk.Telemetry.Sink.AzureMonitor` | 0.1.0 | Azure Monitor / Application Insights sink |

#### Pulse.Collector (Deployable Service)

- OTLP HTTP endpoints (protobuf + JSON) for traces, metrics, and logs
- OTLP gRPC endpoints for traces, metrics, and logs
- Multi-sink fan-out pipeline with per-sink failure isolation
- Grid context enrichment via `TelemetryEnricher`
- Error span/log extraction and routing to Sentry
- Analytics event routing to PostHog with batching
- Transport event publishing (`PulseIngested`) with failure isolation
- OTLP protobuf and JSON parsing with full resource/scope extraction
- Health and readiness endpoints
- Vault integration for sink credentials
- Self-telemetry via `HoneyDrunk.Pulse.Collector` ActivitySource and Meter

#### Architecture

- Three-layer sink pattern: `ITraceSink`, `ILogSink`, `IMetricsSink`, `IAnalyticsSink`, `IErrorSink`
- Each sink independently configurable and toggleable
- Failure in one sink does not affect others
- Transport publish failures isolated from ingestion responses
- Log level filtering for Loki sink via `LokiSinkOptions.MinimumLogLevel`
