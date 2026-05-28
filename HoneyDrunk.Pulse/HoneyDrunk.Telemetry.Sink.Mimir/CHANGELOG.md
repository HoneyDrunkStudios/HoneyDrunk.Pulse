# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.Mimir will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-05-28

### Changed

- MimirSink updated to construct `HttpOtlpSinkOptionsAdapter` with the new `HttpOtlpSinkAuthSecretNames` record (Sonar S107 ctor-param reduction in the shared adapter).

### Internal

- Bumped `HoneyDrunk.Vault` `0.3.0 -> 0.7.0`.
- Bumped `Microsoft.Extensions.*` `10.0.7 -> 10.0.8`.
- See the [repo CHANGELOG](../CHANGELOG.md) for the full release notes.

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
