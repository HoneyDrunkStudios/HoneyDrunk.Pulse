# Changelog

All notable changes to HoneyDrunk.Pulse.Contracts will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-05-28

### Internal

- Package version aligned to `0.4.0` for the Pulse 0.4.0 release train. See the [repo CHANGELOG](../CHANGELOG.md) for the full ADR-0011 D11 Sonar/SAST cleanup, dependency train refresh, and Dockerfile hardening that ship in this release.

## [Unreleased]

### Changed

- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## [0.3.0] - 2026-05-18

### Changed

- Aligned package version to 0.3.0 for the coordinated Kernel adoption release.

## [0.2.0] - 2026-05-04

### Changed

- Released 0.2.0 as part of the ADR-0026 tenancy rollout and solution-wide package version alignment.

## [0.1.0] - 2026-02-21

### Added

- `PulseIngested` event contract for Transport publishing
- `IngestionStatus` enum (`Success`, `PartialSuccess`, `Failed`)
- `TelemetryEvent` model for analytics pipeline events
- `ErrorEvent` model for error tracking
- `ExtractedErrorSpan` and `ExtractedErrorLog` DTOs for error routing
- `TelemetryEventSeverity` enum for severity classification
- Multi-target support: `netstandard2.0`, `net8.0`, `net10.0`
