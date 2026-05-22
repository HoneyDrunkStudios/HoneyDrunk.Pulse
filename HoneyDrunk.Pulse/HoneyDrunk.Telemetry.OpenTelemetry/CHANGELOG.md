# Changelog

All notable changes to HoneyDrunk.Telemetry.OpenTelemetry will be documented in this file.

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

- Serialized analytics event tenant context when emitting events to Pulse.Collector.

### Changed

- Bumped package version to 0.2.0 for the coordinated ADR-0026 tenancy rollout.

## [0.1.0] - 2026-02-21

### Added

- `AddHoneyDrunkOpenTelemetry` service registration extension
- Preconfigured tracing pipeline with OTLP export, ASP.NET Core, and HTTP instrumentation
- Preconfigured metrics pipeline with OTLP export, runtime, and process instrumentation
- Preconfigured logging pipeline with OTLP export
- Grid context enrichment for trace spans (NodeId, StudioId, Environment)
- Configurable via `HoneyDrunk:OpenTelemetry` configuration section
- Depends on OpenTelemetry 1.15.0 and `HoneyDrunk.Kernel.Abstractions` 0.4.0
