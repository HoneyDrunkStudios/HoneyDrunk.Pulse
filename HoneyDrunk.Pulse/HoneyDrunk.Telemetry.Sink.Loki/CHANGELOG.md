# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.Loki will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-05-28

### Changed

- LokiSink updated to construct `HttpOtlpSinkOptionsAdapter` with the new `HttpOtlpSinkAuthSecretNames` record (Sonar S107 ctor-param reduction in the shared adapter).

### Internal

- Bumped `HoneyDrunk.Vault` `0.3.0 -> 0.7.0`.
- Bumped `Microsoft.Extensions.*` `10.0.7 -> 10.0.8`.
- See the [repo CHANGELOG](../CHANGELOG.md) for the full release notes.

## [Unreleased]

### Changed

- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## [0.3.0] - 2026-05-18

### Changed

- Consolidated HTTP OTLP export, custom header, Vault-backed authorization, and retry behavior through the shared Grafana-family sink helper while preserving Loki-specific defaults and logging.

## [0.2.0] - 2026-05-04

### Changed

- Released 0.2.0 as part of the ADR-0026 tenancy rollout and solution-wide package version alignment.

### Changed

- Resolve Loki Basic auth credentials from `ISecretStore` on each export using `Loki--BasicAuth` or `Loki--Username` + `Loki--Password`.

## [0.1.0] - 2026-02-21

### Added

- `LokiSink` implementation of `ILogSink` for Grafana Loki
- OTLP protobuf and JSON forwarding to Loki `/otlp/v1/logs` endpoint
- Configurable minimum log level filtering via `LokiSinkOptions.MinimumLogLevel`
- Multi-tenant support via `X-Scope-OrgID` header
- Basic authentication support
- `LokiSinkOptions` with endpoint, authentication, and filtering configuration
- `AddLokiSink` service registration extension
