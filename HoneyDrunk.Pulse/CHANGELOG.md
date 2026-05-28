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

## [Unreleased]

## [0.4.0] - 2026-05-28

### Changed

- `Pulse.Collector/Program.cs` made non-instantiable via a private constructor (Sonar S1118). Kept the type non-static because `ILogger<Program>` is used as the standard logger category across the OTLP handlers.
- `Pulse.Collector/Telemetry/CollectorTelemetry.cs` is now a `static class` (all members were already static).
- `Pulse.Collector/Endpoints/HealthEndpoints.cs` and `OtlpEndpoints.cs`: extracted constants for the `Health` tag, `application/json`, `application/x-protobuf`, `OTLP` tag, `X-Source-Service` / `X-Source-NodeId` header names, and the `accepted` status (Sonar S1192).
- `Pulse.Collector/Configuration/OtlpEndpointValidationExtensions.cs`: `ValidateOtlpEndpointNotSelfReferencing` cognitive complexity 19 → under 15 by extracting `IsSelfReferencing` and `PointsToSameEndpoint` helpers (Sonar S3776).
- `Pulse.Collector/Enrichment/TelemetryEnricher.cs`: `EnrichErrorEvent` and `EnrichTelemetryEvent` cognitive complexity 19/18 → under 15 by extracting `MergeOperationContextIntoErrorEvent` / `MergeOperationContextIntoTelemetryEvent`. Magic string `"pulse.ingested_at"` extracted to `IngestedAtKey` constant (S1192 × 7).
- `Pulse.Collector/Ingestion/IngestionPipeline.cs`: `"unknown"` magic string extracted to `UnknownSource` constant (S1192 × 6). `Pipeline.cs` ctor (10 deps, all DI-injected) and `Process*Async` methods (8–10 params, all per-channel attribution) carry file-level `S107` + `S3776` suppressions with justification — the per-channel routing in this orchestrator class would be obscured by request-record bundling.
- `Pulse.Collector/Ingestion/IngestionPipeline.Log.cs`: kept `LogLogsBatchFilteredByLevel` taking `string minimumLevel` (LoggerMessage source-gen forbids non-loggable `LogLevel` template parameters); the `.ToString()` at the call site is cheap (enum) and the partial gates message formatting via `IsEnabled` internally.
- `Pulse.Collector/Ingestion/OtlpParser.cs`: magic strings extracted (`AttributesKey`, `ResourceLogsKey`, `ExceptionTypeKey`, `ExceptionMessageKey`, `ExceptionStacktraceKey`); two `foreach`+find loops collapsed to `.FirstOrDefault(...)` (Sonar S3267). File-level `S3776` suppression with justification — OTLP protobuf parsers are inherently branchy (per-OTLP-type mapping with optional fields and dual JSON/protobuf content-type paths); the existing private helpers already capture the natural seams.
- `HoneyDrunk.Telemetry.Sink.Shared/HttpOtlpSinkOptionsAdapter.cs`: 8-param constructor reduced to 6 by bundling the three Vault auth secret names into a new `HttpOtlpSinkAuthSecretNames` record (Sonar S107). Loki, Mimir, and Tempo sink callers updated. Both adapter classes converted to primary constructors (Sonar roslyn-use-primary-constructor).
- `HoneyDrunk.Telemetry.Sink.PostHog/Mapping/PostHogEventMapper.cs`: `foreach` + nested filter folded into `.Where(...)` (Sonar S3267).
- `HoneyDrunk.Telemetry.OpenTelemetry/Enrichment/ActivityEnricher.cs`: the two `EnrichWithGridContext` overloads moved adjacent (Sonar S4136).
- `HoneyDrunk.Telemetry.Sink.AzureMonitor/Extensions/ServiceCollectionExtensions.cs`: the two `AddAzureMonitorSink` overloads moved adjacent (Sonar S4136).
- `HoneyDrunk.Pulse.Sample.Api/Program.cs` and `HoneyDrunk.Pulse.Sample.Worker/Program.cs`: `app.Run()` / `host.Run()` → `await app.RunAsync()` / `await host.RunAsync()` (Sonar async-await rule).

### Security

- `Pulse.Collector/Dockerfile`: `$BUILD_CONFIGURATION` quoted as `"$BUILD_CONFIGURATION"` in `dotnet build` / `dotnet publish` (shellcheck-style finding).
- `Pulse.Collector/Dockerfile`: `COPY . .` annotated to make the bounded-context guarantee explicit. Hardened `.dockerignore` at the build-context root to exclude `bin/`, `obj/`, `out/`, `publish/`, `TestResults/`, `.git/`, `.vs/`, `.github/`, `.env*`, `secrets.json`, `appsettings.Development.json`, `*.pfx`, `*.snk`, tests, samples, and repo docs (Sonar S6470). The previous file had `.git` re-inclusion entries (for SourceLink) that were a defence-in-depth hole; removed.

### Internal

- Onboarded Pulse to SonarQube Cloud (ADR-0011 D11). Wired a `sonarcloud` job in `pr.yml` that calls `HoneyDrunkStudios/HoneyDrunk.Actions/.github/workflows/job-sonarcloud.yml` on both `pull_request` (after `pr-core` succeeds) and `push` to `main` (standalone). PR analysis gates the merge on new-code findings; main-branch analysis populates the SonarCloud Overview dashboard and the leak-period baseline.
- Enabled ADR-0044 Grid Review request workflow and repo-local OpenClaw/Codex review configuration.
- Migrated Pulse tests to AwesomeAssertions, adopted HoneyDrunk.Standards.Tests 0.2.9, and refreshed HoneyDrunk.Standards to 0.2.9 across the solution for ADR-0047 testing alignment.
- Seeded the Pulse coverage baseline and wired the push-to-main coverage baseline ratchet for the Grid PR coverage gate.
- Bumped `HoneyDrunk.Kernel` / `HoneyDrunk.Kernel.Abstractions` `0.7.0 → 0.8.0`.
- Bumped `HoneyDrunk.Transport` / `HoneyDrunk.Transport.AzureServiceBus` / `HoneyDrunk.Transport.InMemory` `0.6.0 → 0.7.1`.
- Bumped `HoneyDrunk.Vault` / `HoneyDrunk.Vault.EventGrid` / `HoneyDrunk.Vault.Providers.AppConfiguration` / `HoneyDrunk.Vault.Providers.AzureKeyVault` `0.3.0 → 0.7.0`.
- Bumped `Microsoft.Extensions.*` `10.0.6/10.0.7 → 10.0.8`.
- Bumped `Microsoft.AspNetCore.Mvc.Testing` `10.0.6 → 10.0.8`.
- Bumped `Grpc.AspNetCore` `2.76.0 → 2.80.0`.
- Bumped `Azure.Monitor.OpenTelemetry.Exporter` `1.7.0 → 1.8.1`.
- Bumped `Sentry` `6.4.1 → 6.6.0`.

## [0.3.0] - 2026-05-18

### Changed

- Updated Pulse.Collector to use Kernel's canonical `WellKnownNodes.Ops.Pulse` fallback (`honeydrunk-pulse`) while preserving `HONEYDRUNK_NODE_ID` override behavior.
- Updated Pulse.Collector dependencies to `HoneyDrunk.Kernel` 0.7.0 and `HoneyDrunk.Transport` 0.6.0 packages for compatible Kernel context contracts.
- Aligned `HoneyDrunk.Kernel.Abstractions` references to 0.7.0 and all package versions to 0.3.0 for the coordinated Kernel adoption release.
- Consolidated duplicated Loki, Mimir, and Tempo HTTP OTLP export/header/auth/retry behavior behind a shared linked helper while preserving sink-specific logging and option defaults.

### Added

- Added collector smoke tests pinning the default runtime Node ID to the canonical Kernel well-known Pulse identity.
- Added sink tests covering shared HTTP OTLP custom header, Basic auth, Authorization header, and retry behavior.

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
