# Changelog

## Unreleased

### Security

- Sanitize SDK trace/log export, analytics emission, collector OTLP fan-out and custom error/analytics events with one shared redaction policy
- Enforce JWT bearer authentication on HTTP and gRPC ingestion when configured; reject anonymous non-Development startup without an explicit isolated-gateway opt-out
- Avoid payload-bearing exception and error-message logging at collector sink boundaries

### Documentation

- Document authentication migration, redaction limits, bounded metric dimensions, proposed retention and actionable delivery/alerting checks

### Compatibility

- Existing production configurations must enable JWT ingestion or deliberately configure an authenticated isolated gateway opt-out before rollout
- SDK log scopes are no longer exported by default; binary OTLP attribute/body values and unknown protobuf fields are not passed through
- In-process exceptions are exported as native Sentry type/value events with sanitized structured frames; raw exception Data and inner objects are withheld, and grouping can differ from prior SDK formatting

## [0.4.1] - 2026-09-26

### Changed

- Refresh stable NuGet dependencies; preserve target frameworks and HoneyDrunk public contracts.

| Dependency | Previous | Updated |
| --- | --- | --- |
| Azure.Monitor.OpenTelemetry.Exporter | 1.8.1 | 1.9.0 |
| Grpc.AspNetCore | 2.80.0 | 2.84.0 |
| Grpc.Tools | 2.80.0 | 2.84.0 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.8 | 10.0.12 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.202 | 10.0.401 |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Hosting | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Http | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.8 | 10.0.12 |
| OpenTelemetry | 1.15.3 | 1.19.1 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.3 | 1.19.1 |
| OpenTelemetry.Extensions.Hosting | 1.15.3 | 1.19.1 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.15.2 | 1.19.0 |
| OpenTelemetry.Instrumentation.Http | 1.15.1 | 1.19.0 |
| OpenTelemetry.Instrumentation.Runtime | 1.15.1 | 1.19.0 |
| Sentry | 6.6.0 | 6.11.1 |


All notable changes to the HoneyDrunk.Pulse repository are documented here. The
format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and
this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Per-package detail lives in each package's own `CHANGELOG.md` under
`HoneyDrunk.Pulse/`.




### Verified HoneyDrunk dependencies

- HoneyDrunk.Kernel: 0.8.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Kernel.Abstractions: 0.8.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Standards: 0.2.9 -> 0.3.0 (verified on NuGet.org).
- HoneyDrunk.Standards.Tests: 0.2.9 -> 0.3.0 (verified on NuGet.org).
- HoneyDrunk.Transport: 0.7.1 -> 0.7.2 (verified on NuGet.org).
- HoneyDrunk.Transport.AzureServiceBus: 0.7.1 -> 0.7.2 (verified on NuGet.org).
- HoneyDrunk.Transport.InMemory: 0.7.1 -> 0.7.2 (verified on NuGet.org).
- HoneyDrunk.Vault: 0.8.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Vault.EventGrid: 0.8.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Vault.Providers.AppConfiguration: 0.8.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Vault.Providers.AzureKeyVault: 0.8.0 -> 0.8.1 (verified on NuGet.org).

## Unreleased

## 0.4.0 - Sonar + SAST cleanup

### Added

- Onboarded Pulse to SonarQube Cloud (ADR-0011 D11) with a `sonarcloud` job in `pr.yml` gating PRs on new-code findings and populating the main-branch dashboard.
- Enabled the ADR-0086 Grid Review request workflow and the local Grid review worker.
- Migrated the test suite to AwesomeAssertions and seeded the coverage baseline ratchet for the Grid PR coverage gate.

### Changed

- Hardened `Pulse.Collector` for Sonar findings: private `Program` constructor, static telemetry helpers, extracted magic-string constants, and reduced cognitive complexity in OTLP validation, enrichment, ingestion, and parsing paths.
- Consolidated Loki, Mimir, and Tempo HTTP OTLP auth secret names behind a shared `HttpOtlpSinkAuthSecretNames` record and converted sink adapters to primary constructors.
- Switched the sample apps to `await app.RunAsync()` / `await host.RunAsync()`.

### Security

- Hardened the `Pulse.Collector` Docker build context and `.dockerignore` to exclude secrets, dev settings, tests, samples, and repo docs (Sonar S6470).

## 0.3.0 - Kernel adoption

### Added

- Added collector smoke tests pinning the default Node ID to the canonical Kernel well-known Pulse identity.
- Added sink tests covering shared HTTP OTLP custom header, Basic auth, Authorization header, and retry behavior.

### Changed

- Adopted Kernel's canonical `WellKnownNodes.Ops.Pulse` fallback while preserving the `HONEYDRUNK_NODE_ID` override.
- Aligned `HoneyDrunk.Kernel.Abstractions` to 0.7.0 and all package versions to 0.3.0 for the coordinated Kernel release.
- Consolidated duplicated Loki, Mimir, and Tempo HTTP OTLP behavior behind a shared linked helper.

## 0.2.0 - Vault + tenant context

### Added

- Added ADR-0026 tenant context propagation for collector ingress, analytics events, error reports, and low-cardinality collector telemetry tags.
- Added a PostHog credential rotation canary covering per-flush Vault secret resolution.

### Changed

- Migrated the collector bootstrap to env-driven Azure Key Vault and Azure App Configuration discovery using the `pulse` label.
- Registered the Vault Event Grid cache invalidation webhook at `/internal/vault/invalidate`.
- Moved PostHog, Sentry, Loki, Tempo, and Mimir sink credentials behind `ISecretStore`.
- Switched the deploy workflow to Azure OIDC.

## 0.1.0 - Initial release

### Added

- First public release of the HoneyDrunk.Pulse telemetry ecosystem: telemetry abstractions, OpenTelemetry integration, the multi-backend sink pipeline (Loki, Tempo, Mimir, PostHog, Sentry, Azure Monitor), shared `PulseIngested` contracts, and the Pulse Collector OTLP receiver (HTTP + gRPC) with per-sink failure isolation.
