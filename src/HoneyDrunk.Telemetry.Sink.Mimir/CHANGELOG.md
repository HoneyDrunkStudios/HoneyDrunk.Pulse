# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.Mimir will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.2] - 2026-11-05

### Changed

- Refresh NuGet dependencies and adopt upstream package releases in dependency order. Preserve target frameworks and public contracts.

| Dependency | Previous | Updated |
| --- | --- | --- |
| OpenTelemetry.Instrumentation.Process | 1.14.0-beta.2 | 1.20.1-rc.1 |
| Azure.Monitor.OpenTelemetry.Exporter | 1.9.0 | 1.10.0 |
| Grpc.AspNetCore | 2.84.0 | 2.85.0 |
| Grpc.Tools | 2.84.0 | 2.85.0 |
| HoneyDrunk.Kernel | 0.8.1 | 0.8.2 |
| HoneyDrunk.Kernel.Abstractions | 0.8.1 | 0.8.2 |
| HoneyDrunk.Standards | 0.3.0 | 0.4.3 |
| HoneyDrunk.Standards.Tests | 0.3.0 | 0.4.3 |
| HoneyDrunk.Transport | 0.7.2 | 0.7.3 |
| HoneyDrunk.Transport.AzureServiceBus | 0.7.2 | 0.7.3 |
| HoneyDrunk.Transport.InMemory | 0.7.2 | 0.7.3 |
| HoneyDrunk.Vault | 0.8.1 | 0.8.2 |
| HoneyDrunk.Vault.EventGrid | 0.8.1 | 0.8.2 |
| HoneyDrunk.Vault.Providers.AppConfiguration | 0.8.1 | 0.8.2 |
| HoneyDrunk.Vault.Providers.AzureKeyVault | 0.8.1 | 0.8.2 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.12 | 10.0.14 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.12 | 10.0.14 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.401 | 10.0.403 |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Hosting | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Http | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.12 | 10.0.14 |
| OpenTelemetry | 1.19.1 | 1.20.1 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.19.1 | 1.20.1 |
| OpenTelemetry.Extensions.Hosting | 1.19.1 | 1.20.1 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.19.0 | 1.20.1 |
| OpenTelemetry.Instrumentation.Http | 1.19.0 | 1.20.2 |
| OpenTelemetry.Instrumentation.Runtime | 1.19.0 | 1.20.1 |
| Sentry | 6.11.1 | 6.12.1 |

## [0.4.1] - 2026-09-26

### Changed

- Refresh dependency and shared build-tooling versions; preserve target frameworks and existing public contracts. See the [repository dependency changes](../../CHANGELOG.md).

## [0.4.0] - 2026-05-28

### Changed

- MimirSink updated to construct `HttpOtlpSinkOptionsAdapter` with the new `HttpOtlpSinkAuthSecretNames` record (Sonar S107 ctor-param reduction in the shared adapter).

### Internal

- Bumped `HoneyDrunk.Vault` `0.3.0 -> 0.7.0`.
- Bumped `Microsoft.Extensions.*` `10.0.7 -> 10.0.8`.
- See the [repo CHANGELOG](../../HoneyDrunk.Pulse/CHANGELOG.md) for the full release notes.

## [Unreleased]

### Changed

- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## [0.3.0] - 2026-05-18

### Changed

- Consolidated HTTP OTLP export, custom header, Vault-backed authorization, and retry behavior through the shared Grafana-family sink helper while preserving Mimir-specific defaults and logging.

## [0.2.0] - 2026-05-04

### Changed

- Released 0.2.0 as part of the ADR-0026 tenancy rollout and solution-wide package version alignment.

### Changed

- Resolve Mimir Basic auth credentials from `ISecretStore` on each export using `Mimir--BasicAuth` or `Mimir--Username` + `Mimir--Password`.

## [0.1.0] - 2026-02-21

### Added

- `MimirSink` implementation of `IMetricsSink` for Grafana Mimir
- OTLP protobuf and JSON forwarding to Mimir `/otlp/v1/metrics` endpoint
- Multi-tenant support via `X-Scope-OrgID` header
- Basic authentication support
- `MimirSinkOptions` with endpoint and authentication configuration
- `AddMimirSink` service registration extension
