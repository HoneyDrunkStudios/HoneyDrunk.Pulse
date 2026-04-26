# Changelog

All notable changes to HoneyDrunk.Pulse.Contracts will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-02-21

### Added

- `PulseIngested` event contract for Transport publishing
- `IngestionStatus` enum (`Success`, `PartialSuccess`, `Failed`)
- `TelemetryEvent` model for analytics pipeline events
- `ErrorEvent` model for error tracking
- `ExtractedErrorSpan` and `ExtractedErrorLog` DTOs for error routing
- `TelemetryEventSeverity` enum for severity classification
- Multi-target support: `netstandard2.0`, `net8.0`, `net10.0`
