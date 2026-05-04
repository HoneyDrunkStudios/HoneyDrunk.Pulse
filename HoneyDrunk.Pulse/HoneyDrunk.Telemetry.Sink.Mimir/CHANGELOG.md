# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.Mimir will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
