# Changelog

All notable changes to HoneyDrunk.Telemetry.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
