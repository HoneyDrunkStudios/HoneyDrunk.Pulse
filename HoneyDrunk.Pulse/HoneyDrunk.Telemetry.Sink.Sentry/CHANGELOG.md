# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.Sentry will be documented in this file.

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

### Changed

- Replaced configured Sentry DSN values with `DsnSecretName`, defaulting to `Sentry--Dsn`.
- Resolve the Sentry DSN from `ISecretStore` and reinitialize the SDK when Vault returns a new value/version.

## [0.1.0] - 2026-02-21

### Added

- `SentrySink` implementation of `IErrorSink` for Sentry
- Error event capture with severity mapping
- Exception details forwarding (type, message, stacktrace)
- Tag and extra data propagation
- Grid context enrichment (CorrelationId, NodeId, Environment)
- `SentrySinkOptions` with DSN and environment configuration
- `AddSentrySink` service registration extension
- Depends on Sentry SDK 6.1.0
