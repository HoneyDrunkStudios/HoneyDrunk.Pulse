# Changelog

All notable changes to HoneyDrunk.Telemetry.Sink.PostHog will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Replaced configured PostHog API key values with `ApiKeySecretName`, defaulting to `PostHog--ApiKey`.
- Resolve the PostHog API key from `ISecretStore` for each flush so rotation propagates without restart.

## [0.1.0] - 2026-02-21

### Added

- `PostHogSink` implementation of `IAnalyticsSink` for PostHog
- HTTP-based batch event capture via `/batch` endpoint
- Configurable batch size and flush interval
- Event mapping from `TelemetryEvent` to PostHog capture format
- Property filtering via approved/excluded key lists
- Retry with exponential backoff for transient failures
- HTTP 429 (Too Many Requests) handling with `Retry-After` header support
- `PostHogSinkOptions` with API key, host, batching, and retry configuration
- `AddPostHogSink` service registration extension
