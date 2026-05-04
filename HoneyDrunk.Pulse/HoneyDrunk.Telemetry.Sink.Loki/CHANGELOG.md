# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.Loki will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
