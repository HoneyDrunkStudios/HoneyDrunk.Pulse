# Changelog

All notable changes to HoneyDrunk.Telemetry.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## [0.3.0] - 2026-05-18

### Changed

- Updated `HoneyDrunk.Kernel.Abstractions` dependency to 0.7.0 for canonical Grid identity/context contracts.

## [0.2.0] - 2026-05-04

### Added

- Documented ADR-0026 `tenant_id` as a bounded, low-cardinality telemetry tag.

### Changed

- Bumped package version to 0.2.0 for the coordinated ADR-0026 tenancy rollout.

### Added

- Documented ADR-0026 `tenant_id` as a bounded, low-cardinality telemetry tag.

## [0.1.0] - 2026-02-21

### Added

- `ITraceSink` interface for trace data forwarding (OTLP protobuf/JSON)
- `ILogSink` interface for log data forwarding with severity filtering
- `IMetricsSink` interface for metrics data forwarding
- `IAnalyticsSink` interface for product analytics event capture
- `IErrorSink` interface for error event routing
- `TelemetryEvent` model with properties, tags, and severity
- `ErrorEvent` model with exception details, tags, and extra data
- Depends on `HoneyDrunk.Kernel.Abstractions` 0.4.0 for Grid identity types
