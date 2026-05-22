# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.AzureMonitor will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

- `AzureMonitorSink` implementing `ITraceSink`, `ILogSink`, and `IMetricsSink`
- OTLP data forwarding to Application Insights via Azure Monitor OpenTelemetry Exporter
- Connection string configuration via `AzureMonitorSinkOptions`
- `AddAzureMonitorSink` service registration extension
- Depends on Azure.Monitor.OpenTelemetry.Exporter 1.6.0
