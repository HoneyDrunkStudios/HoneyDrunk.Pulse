# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.Tempo will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Resolve Tempo Basic auth credentials from `ISecretStore` on each export using `Tempo--BasicAuth` or `Tempo--Username` + `Tempo--Password`.

## [0.1.0] - 2026-02-21

### Added

- `TempoSink` implementation of `ITraceSink` for Grafana Tempo
- OTLP protobuf and JSON forwarding to Tempo `/otlp/v1/traces` endpoint
- Multi-tenant support via `X-Scope-OrgID` header
- Basic authentication support
- `TempoSinkOptions` with endpoint and authentication configuration
- `AddTempoSink` service registration extension
