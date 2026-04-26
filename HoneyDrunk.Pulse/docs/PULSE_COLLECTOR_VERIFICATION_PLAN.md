# Pulse.Collector — Verification Plan

**Node:** Pulse.Collector  
**Version:** Phase 1  
**Date:** 2026-02-21  
**Status:** Draft — pending Go/No-Go execution  

---

## 1. System Map

```
┌───────────────────────────────────────────────────────────────────────┐
│  PRODUCERS                                                            │
│                                                                       │
│  ┌─────────────────────┐   ┌─────────────────────┐                    │
│  │ Sample.Api           │   │ Sample.Worker        │                   │
│  │ (OTEL SDK)           │   │ (OTEL SDK)           │                   │
│  │ + PulseAnalytics     │   │ + PulseAnalytics     │                   │
│  │   Emitter            │   │   Emitter            │                   │
│  └────────┬─────────────┘   └────────┬─────────────┘                   │
│           │                          │                                │
│     HTTP/gRPC OTLP              HTTP/gRPC OTLP                       │
│     + X-Source-Service          + X-Source-Service                    │
│     + X-Source-NodeId           + X-Source-NodeId                     │
└───────────┼──────────────────────────┼────────────────────────────────┘
            │                          │
            ▼                          ▼
┌───────────────────────────────────────────────────────────────────────┐
│  PULSE.COLLECTOR  (ASP.NET Core + gRPC)                              │
│                                                                       │
│  ┌─ HTTP Endpoints ──────────────────────────────────────────────┐    │
│  │  POST /otlp/v1/traces    (proto | json)                      │    │
│  │  POST /otlp/v1/metrics   (proto | json)                      │    │
│  │  POST /otlp/v1/logs      (proto | json)                      │    │
│  │  POST /otlp/v1/analytics (json only — custom analytics)      │    │
│  │  POST /otlp/v1/errors    (json only — error reporting)       │    │
│  └───────────────────────────────────────────────────────────────┘    │
│                                                                       │
│  ┌─ gRPC Services ───────────────────────────────────────────────┐    │
│  │  OtlpTraceService    → TraceService.TraceServiceBase          │    │
│  │  OtlpMetricsService  → MetricsService.MetricsServiceBase     │    │
│  │  OtlpLogsService     → LogsService.LogsServiceBase           │    │
│  └───────────────────────────────────────────────────────────────┘    │
│                                                                       │
│  ┌─ Pipeline ────────────────────────────────────────────────────┐    │
│  │  OtlpParser  ──▶  TelemetryEnricher  ──▶  IngestionPipeline  │    │
│  │  (parse/count)    (Grid metadata)        (route to sinks)     │    │
│  └───────────────────────────────────────────────────────────────┘    │
│                                                                       │
│  ┌─ Health ──────────────────────────────────────────────────────┐    │
│  │  GET /health    GET /ready    GET /live                       │    │
│  └───────────────────────────────────────────────────────────────┘    │
│                                                                       │
│  ┌─ Self-Telemetry ──────────────────────────────────────────────┐    │
│  │  ActivitySource: HoneyDrunk.Pulse.Collector                   │    │
│  │  Meter: HoneyDrunk.Pulse.Collector.Metrics                    │    │
│  │  Counters: traces/metrics/logs/analytics.ingested, errors     │    │
│  │  Histogram: processing.duration                               │    │
│  └───────────────────────────────────────────────────────────────┘    │
└──────────┬──────────┬──────────┬──────────┬──────────┬────────────────┘
           │          │          │          │          │
           ▼          ▼          ▼          ▼          ▼
┌──────────────────────────────────────────────────────────────────────┐
│  SINKS                                                               │
│                                                                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────┐  │
│  │  Tempo   │ │  Loki    │ │  Mimir   │ │ Sentry   │ │  PostHog  │  │
│  │ ITrace   │ │ ILogSink │ │ IMetrics │ │ IError   │ │ IAnalytics│  │
│  │  Sink    │ │          │ │  Sink    │ │  Sink    │ │  Sink     │  │
│  │ HTTP PUT │ │ HTTP PUT │ │ HTTP PUT │ │ SDK call │ │ POST /bat │  │
│  │ raw OTLP │ │ raw OTLP │ │ raw OTLP │ │          │ │ ch (HTTP) │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └───────────┘  │
│                                                                      │
│  ┌───────────────────┐                                               │
│  │  Azure Monitor    │  ITraceSink + ILogSink + IMetricsSink         │
│  │  (all 3 signals)  │  Connection string via Vault                  │
│  └───────────────────┘                                               │
│                                                                      │
│  ┌───────────────────┐                                               │
│  │  HoneyDrunk       │  PulseIngestedPublisher → IMessagePublisher   │
│  │  Transport        │  Topic: "pulse-ingested"                      │
│  │  (Grid events)    │  Adapters: InMemory (dev) | ServiceBus (prod) │
│  └───────────────────┘                                               │
└──────────────────────────────────────────────────────────────────────┘
```

### Protocol Expectations

| Endpoint | Protocol | Content-Types | Auth (Phase 1) |
|---|---|---|---|
| `/otlp/v1/traces` | HTTP POST | `application/x-protobuf`, `application/json` | Optional (`RequireOtlpAuthentication`) |
| `/otlp/v1/metrics` | HTTP POST | `application/x-protobuf`, `application/json` | Same |
| `/otlp/v1/logs` | HTTP POST | `application/x-protobuf`, `application/json` | Same |
| `/otlp/v1/analytics` | HTTP POST | `application/json` | Same |
| `/otlp/v1/errors` | HTTP POST | `application/json` | Same |
| gRPC `TraceService/Export` | gRPC | protobuf | Same |
| gRPC `MetricsService/Export` | gRPC | protobuf | Same |
| gRPC `LogsService/Export` | gRPC | protobuf | Same |
| `/health`, `/ready`, `/live` | HTTP GET | — | None |

### Sink Routing Matrix

| Signal | Sink(s) | Interface | Transport |
|---|---|---|---|
| Traces (OTLP) | Tempo, AzureMonitor | `ITraceSink.ExportAsync(byte[], contentType)` | Raw OTLP passthrough over HTTP |
| Traces (errors) | Sentry | `IErrorSink.CaptureAsync(ErrorEvent)` | Sentry SDK |
| Metrics (OTLP) | Mimir, AzureMonitor | `IMetricsSink.ExportAsync(byte[], contentType)` | Raw OTLP passthrough over HTTP |
| Logs (OTLP) | Loki, AzureMonitor | `ILogSink.ExportAsync(byte[], contentType)` | Raw OTLP passthrough over HTTP |
| Logs (errors) | Sentry | `IErrorSink.CaptureAsync(ErrorEvent)` | Sentry SDK |
| Analytics | PostHog | `IAnalyticsSink.CaptureBatchAsync(events)` | HTTP POST /batch |
| Errors | Sentry | `IErrorSink.CaptureAsync(ErrorEvent)` | Sentry SDK |
| All signals | Transport | `PulseIngestedPublisher` | InMemory / ServiceBus |

---

## 2. Verification Checklist

Execute in order. Each step has a clear pass/fail gate.

### A. Baseline — Build & Existing Tests

| # | Step | Pass Criteria |
|---|---|---|
| A.1 | `dotnet build HoneyDrunk.Pulse.slnx -c Release` | Zero errors, zero warnings |
| A.2 | `dotnet test HoneyDrunk.Pulse.slnx -c Release --no-build` | All 128 existing tests green |
| A.3 | Verify `Pulse.Collector` starts locally | `/health` returns `200 { Status: "Healthy" }` |
| A.4 | Verify `/ready` and `/live` return 200 | Both return expected JSON |

### B. OTLP Intake — HTTP

| # | Step | Pass Criteria |
|---|---|---|
| B.1 | POST valid protobuf `ExportTraceServiceRequest` to `/otlp/v1/traces` | 200, response includes `SpanCount > 0` |
| B.2 | POST valid JSON OTLP traces to `/otlp/v1/traces` with `Content-Type: application/json` | 200, `SpanCount > 0` |
| B.3 | POST valid protobuf metrics to `/otlp/v1/metrics` | 200, `MetricCount > 0` |
| B.4 | POST valid protobuf logs to `/otlp/v1/logs` | 200, `LogCount > 0` |
| B.5 | POST analytics event JSON to `/otlp/v1/analytics` | 200, response includes `Count > 0` |
| B.6 | POST error report JSON to `/otlp/v1/errors` | 200, `{ Status: "accepted" }` |
| B.7 | POST analytics with `SourceService` / `SourceNodeId` in JSON body | `PulseIngested.SourceNodeName` and `SourceNodeId` from body (takes precedence over headers) |
| B.8 | Verify `X-Source-Service` header flows through to `sourceName` in pipeline | `PulseIngested.SourceNodeName` matches header value |
| B.9 | Verify `X-Source-NodeId` header flows through | `PulseIngested.SourceNodeId` matches header value |

### C. OTLP Intake — gRPC

| # | Step | Pass Criteria |
|---|---|---|
| C.1 | gRPC `TraceService/Export` with valid `ExportTraceServiceRequest` | gRPC OK status, empty `ExportTraceServiceResponse` returned (OTLP-compliant — no counts in gRPC response) |
| C.2 | gRPC `MetricsService/Export` | gRPC OK status, empty `ExportMetricsServiceResponse` |
| C.3 | gRPC `LogsService/Export` | gRPC OK status, empty `ExportLogsServiceResponse` |
| C.4 | gRPC with invalid request → `StatusCode.Internal` | `RpcException` with `StatusCode.Internal` and message "Error processing {type}" |
| C.5 | Verify gRPC metadata headers (`x-source-service`, `x-source-nodeid`) flow | Same as B.7/B.8 via gRPC |

### D. Enrichment — Grid Metadata

| # | Step | Pass Criteria |
|---|---|---|
| D.1 | Incoming telemetry without `service.name` → enricher adds `"unknown-service"` | Verify enriched attributes contain `service.name = "unknown-service"` |
| D.2 | Incoming telemetry with `service.name` → enricher preserves it | Original value unchanged |
| D.3 | `honeydrunk.node_id` injected from `IOperationContextAccessor` | Attribute present with Collector's NodeId (`pulse-collector`) |
| D.4 | `honeydrunk.correlation_id` injected from operation context | Attribute present when context exists |
| D.5 | `honeydrunk.operation_id` injected | Present when context exists |
| D.6 | `honeydrunk.environment` injected | Matches `PulseCollectorOptions.Environment` |
| D.7 | `pulse.ingested_at` timestamp added | Present, value is Unix timestamp in ms |
| D.8 | Enrichment is additive (no existing attributes overwritten) | Pre-existing keys survive enrichment |
| D.9 | Analytics event enrichment adds `NodeId`, `TenantId`, `CorrelationId` | All present post-enrichment |
| D.10 | Error event enrichment sets `NodeId`, `Environment`, `CorrelationId` | All present post-enrichment |

### E. Sink Routing

| # | Step | Pass Criteria |
|---|---|---|
| E.1 | Traces with error spans → Sentry receives `ErrorEvent` | `IErrorSink.CaptureAsync` called with error message, trace.id, span.id tags |
| E.2 | Traces raw OTLP → Tempo receives `ExportAsync(byte[], contentType)` | `ITraceSink.ExportAsync` called with identical bytes |
| E.3 | Metrics raw OTLP → Mimir receives `ExportAsync(byte[], contentType)` | `IMetricsSink.ExportAsync` called with identical bytes |
| E.4 | Logs raw OTLP → Loki receives `ExportAsync(byte[], contentType)` | `ILogSink.ExportAsync` called with identical bytes |
| E.5 | Logs with error records → Sentry receives `ErrorEvent` per error log | One `CaptureAsync` per error log record |
| E.6 | Analytics events → PostHog receives `CaptureBatchAsync(events)` | `IAnalyticsSink.CaptureBatchAsync` called with enriched events |
| E.7 | Error report → Sentry receives `CaptureAsync(ErrorEvent)` | Enriched ErrorEvent forwarded |
| E.8 | Disabled sink (e.g., `EnableTempoSink=false`) → sink not called | No `ITraceSink` registered, no call made |
| E.9 | All signals → `PulseIngested` event published to Transport | `IMessagePublisher.PublishAsync` called with correct counts, metadata |
| E.10 | AzureMonitor sink (when enabled) receives traces + logs + metrics | All three `ExportAsync` methods called |
| E.11 | Loki minimum log level filtering works | Batch below threshold → `ExportAsync` not called |

### F. Transport Integration

| # | Step | Pass Criteria |
|---|---|---|
| F.1 | `PulseIngested` event has correct `Version` (1) | `Version == PulseContractVersions.Current` |
| F.2 | `PulseIngested.BatchId` is unique per invocation | Each call produces a different GUID-based ID |
| F.3 | `PulseIngested.Metadata` contains enrichment tags | Keys: `service.name`, `honeydrunk.environment`, `honeydrunk.correlation_id`, `pulse.ingested_at` |
| F.4 | Grid context set on published message | `GridContext.NodeId` = `pulse-collector`, `CorrelationId` present |
| F.5 | Transport disabled (`EnableTransportPublishing=false`) → no publish | `IMessagePublisher.PublishAsync` not called |

### G. Security / PII

| # | Step | Pass Criteria |
|---|---|---|
| G.1 | Secret values (Vault DSN, API keys) never appear in logs | Grep structured logs for known secret patterns — zero matches |
| G.2 | Secret values never appear in telemetry attributes | Inspect enriched attributes — no Vault secrets |
| G.3 | PostHog approved/excluded key lists work | Excluded keys stripped, only approved keys survive when list provided |
| G.4 | Stack traces forwarded to Sentry do not contain secrets | Inspect `Extra["exception.stacktrace"]` — no secret values |
| G.5 | `SecretValidationExtensions` fail-fast in production when secrets missing | Application startup throws `InvalidOperationException` |

### H. Configuration Validation (Fail-Fast)

| # | Step | Pass Criteria |
|---|---|---|
| H.1 | Production without OTLP endpoint → startup failure | `InvalidOperationException` thrown |
| H.2 | Production with localhost OTLP → startup failure | `InvalidOperationException` thrown |
| H.3 | Production with InMemory transport adapter → startup failure | `InvalidOperationException` thrown |
| H.4 | Production with ServiceBus but no queue name → startup failure | `InvalidOperationException` thrown |
| H.5 | OTLP endpoint pointing to self → startup failure | Self-reference detection fires |
| H.6 | Production with enabled sinks but missing endpoint config → startup failure | Sink validation fires |
| H.7 | Development gracefully degrades (localhost, InMemory) | Starts successfully with warnings |

---

## 3. Test Matrix

### 3.1 Happy Path

| Test Name | Signal | Input | Expected Enrichment | Expected Sink Outcome | Failure Mode |
|---|---|---|---|---|---|
| `Traces_Protobuf_RoutesToTempo` | Trace | Valid `ExportTraceServiceRequest` (protobuf, 3 spans) | `service.name`, `honeydrunk.environment`, `pulse.ingested_at` | Tempo `ExportAsync` called with raw bytes; `PulseIngested` published with `TraceCount=3` | Tempo HTTP 5xx → `PartialSuccess` status |
| `Traces_Json_ParsesAccurately` | Trace | Valid OTLP JSON with 2 resourceSpans, 5 spans | `service.name` extracted from resource | Response `SpanCount=5`; parser counts match | Malformed JSON → `SpanCount=0`, no crash |
| `Traces_ErrorSpan_ForwardsToSentry` | Trace | Protobuf with 1 error span (status=ERROR + exception event) | N/A | Sentry `CaptureAsync` with message, `trace.id` tag, `exception.type` extra | Sentry unreachable → logged, no crash |
| `Metrics_Protobuf_RoutesToMimir` | Metric | Valid `ExportMetricsServiceRequest` (protobuf, 10 data points) | `service.name`, `pulse.ingested_at` | Mimir `ExportAsync` called; `PulseIngested` with `MetricCount=10` | Mimir HTTP 5xx → `PartialSuccess` |
| `Logs_Protobuf_RoutesToLoki` | Log | Valid `ExportLogsServiceRequest` (protobuf, 8 log records) | `service.name`, `pulse.ingested_at` | Loki `ExportAsync` called; `PulseIngested` with `LogCount=8` | Loki HTTP 5xx → `PartialSuccess` |
| `Logs_ErrorRecord_ForwardsToSentry` | Log | Protobuf with 2 error logs (severity ≥ 17) | `honeydrunk.correlation_id` from trace_id | Sentry `CaptureAsync` called 2×, severity mapped correctly | Sentry unreachable → logged only |
| `Logs_BelowMinLevel_FilteredForLoki` | Log | Protobuf with `maxSeverity=5` (DEBUG), Loki min=Warning | N/A | Loki `ExportAsync` NOT called; AzureMonitor (if enabled) still receives | None — filtered by design |
| `Analytics_ValidBatch_RoutesToPostHog` | Analytics | JSON with 3 `TelemetryEvent`s, `EventName` set | `honeydrunk.environment`, `honeydrunk.correlation_id`, `NodeId` | PostHog `CaptureBatchAsync` called with 3 enriched events | PostHog HTTP 5xx → PartialSuccess |
| `Error_ValidReport_RoutesToSentry` | Error | JSON `ErrorReportRequest` with exception message | `Environment`, `NodeId`, `CorrelationId` enriched | Sentry `CaptureAsync` with enriched `ErrorEvent` | Sentry unreachable → logged only |
| `gRPC_Traces_EndToEnd` | Trace | gRPC `Export` with metadata headers | Same as Traces_Protobuf | gRPC OK status (empty response — no counts); sinks receive data; `PulseIngested` published | gRPC `StatusCode.Internal` with error message |
| `Transport_PulseIngested_Published` | All | Any successful ingestion | Metadata dict populated | `IMessagePublisher.PublishAsync` called with `PulseIngested` | Transport error → does not block ingestion |
| `AzureMonitor_AllSignals_Routed` | All | Enable AzureMonitor sink; send trace + metric + log | N/A | All three `ExportAsync` methods called on `AzureMonitorSink` | Connection string missing → fail-fast |

### 3.2 Enrichment Deep-Dive

| Test Name | Signal | Input | Expected Enrichment | Expected Sink Outcome | Failure Mode |
|---|---|---|---|---|---|
| `Enrich_MissingServiceName_DefaultsToUnknown` | Trace | Attributes without `service.name` | `service.name = "unknown-service"` added | Downstream sees default service name | N/A |
| `Enrich_ExistingServiceName_Preserved` | Trace | Attributes with `service.name = "my-api"` | `service.name = "my-api"` unchanged | Correct name forwarded | N/A |
| `Enrich_GridContext_AllFieldsInjected` | All | Operation context with full GridContext | `honeydrunk.node_id`, `honeydrunk.correlation_id`, `honeydrunk.operation_id`, `honeydrunk.tenant_id`, `honeydrunk.environment` all present | Metadata enriched | N/A |
| `Enrich_NoOperationContext_Graceful` | All | No `IOperationContextAccessor.Current` | Only collector metadata (`pulse.ingested_at`, `honeydrunk.environment`) | No crash, partial enrichment | N/A |
| `Enrich_IngestTimestamp_AlwaysAdded` | All | Any payload | `pulse.ingested_at` = Unix ms timestamp | Verifiable in metadata | N/A |
| `Enrich_Additive_NeverOverwrites` | All | Pre-existing `honeydrunk.node_id = "original"` | `honeydrunk.node_id` remains `"original"` | Original value forwarded | N/A |

### 3.3 Negative / Failure Tests

| Test Name | Signal | Input | Expected Enrichment | Expected Sink Outcome | Failure Mode |
|---|---|---|---|---|---|
| `Traces_EmptyBody_Returns200Empty` | Trace | Empty HTTP body | N/A | `SpanCount=0`, no sink calls | Graceful — OtlpParser returns `Empty` |
| `Traces_MalformedProtobuf_Graceful` | Trace | Random 500 bytes, `Content-Type: application/x-protobuf` | N/A | Parser heuristic fallback; no crash | Parser exception caught, logged |
| `Traces_MalformedJson_Graceful` | Trace | `{invalid json}`, `Content-Type: application/json` | N/A | Parser fallback; `SpanCount=0` | `LogTraceParseError` emitted |
| `Analytics_EmptyEvents_Rejected` | Analytics | `{ "events": [] }` | N/A | 400 Bad Request `{ "error": "No events provided" }` | Input validation |
| `Analytics_NullEvents_Rejected` | Analytics | `{}` (no events field) | N/A | 400 Bad Request `{ "error": "No events provided" }` | Input validation |
| `Analytics_InvalidJson_400` | Analytics | `not json at all` | N/A | 400 Bad Request `{ "error": "Invalid JSON format" }` | `JsonException` caught |
| `Error_NullBody_400` | Error | `null` or empty body | N/A | 400 Bad Request `{ "error": "Invalid request" }` | Input validation |
| `Error_InvalidJson_400` | Error | `not json at all` | N/A | 400 Bad Request `{ "error": "Invalid JSON format" }` | `JsonException` caught |
| `SinkUnreachable_Tempo_PartialSuccess` | Trace | Valid traces, Tempo returns HTTP 503 | Normal enrichment | `PulseIngested.Status = PartialSuccess`, `ErrorMessage` set | Logged, other sinks still called |
| `SinkUnreachable_AllSinks_PartialSuccess` | Trace | Valid traces, all sinks throw | Normal enrichment | `PulseIngested.Status = PartialSuccess` | All failures logged independently |
| `SinkThrottled_429_Logged` | Trace | Valid traces, sink returns HTTP 429 | Normal enrichment | Sink retry logic activates (if present); otherwise `PartialSuccess` | Backoff logged |
| `OversizedPayload_LargeTrace` | Trace | 10 MB protobuf payload | Normal enrichment | Processed (up to `MaxBatchSize` or server limits) | Kestrel defaults may reject >28 MB |
| `HighVolumeBurst_100Requests` | All | 100 concurrent ingestion requests | All enriched | All processed; no deadlocks; `processing.duration` histogram updated | Request queuing, no data loss |
| `MissingCorrelation_NoContext` | All | No `IOperationContextAccessor.Current`; no `X-Source-*` headers | Only collector defaults (`pulse.ingested_at`, `environment`) | `PulseIngested.SourceNodeName = null` | Expected — enrichment is best-effort |
| `MissingNodeId_NoGridContext` | All | No GridContext available | No `honeydrunk.node_id` in enriched attributes | Partial metadata | Expected — Grid context optional on ingestion path |
| `TransportError_PropagatesAndFails` | All | Valid traces; `IMessagePublisher.PublishAsync` throws | Normal enrichment | Sinks receive data BEFORE publish step; pipeline throws; HTTP 500 returned | **Known gap:** `PulseIngestedPublisher.PublishAsync` does NOT catch exceptions. Transport errors propagate through `ProcessTracesAsync` catch block and cause HTTP 500. Sinks are called before publish, so sink delivery succeeds but request fails. |
| `TransportError_SentryNotAffected` | Error | Error event; `IMessagePublisher.PublishAsync` throws | Normal enrichment | Sentry receives error BEFORE publish step | `ProcessErrorAsync` swallows all exceptions (unique among pipeline methods), so error processing is isolated from transport failures |

---

## 4. Demo Harness Design

### 4.1 Architecture

```
┌──────────────────────────────┐
│  Demo Harness                │
│                              │
│  ┌────────────────────────┐  │
│  │ Sample.Api              │  │  ← Emits traces, metrics, logs, analytics via OTEL SDK
│  │ (existing project)      │  │     + PulseAnalyticsEmitter
│  │ GET /weatherforecast    │  │     Configured with OTLP endpoint → Pulse.Collector
│  │ GET /error              │  │
│  │ POST /analytics/*       │  │
│  └────────┬───────────────┘  │
│           │ OTLP (HTTP/gRPC) │
│           ▼                  │
│  ┌────────────────────────┐  │
│  │ Pulse.Collector         │  │  ← All sinks enabled, pointed at fake receivers
│  │ (existing project)      │  │     InMemory transport adapter
│  └───┬────┬────┬────┬────┘  │
│      │    │    │    │        │
│      ▼    ▼    ▼    ▼        │
│  ┌──────────────────────┐    │
│  │ FakeSinkReceiver      │   │  ← Minimal ASP.NET Core app
│  │                       │   │     Accepts OTLP passthrough at configurable paths
│  │  POST /v1/traces      │   │     Stores received payloads in ConcurrentBag
│  │  POST /v1/metrics     │   │     Exposes GET /received/{signal} for assertions
│  │  POST /v1/logs        │   │     Can simulate: 503, 429, timeouts, slow responses
│  │  POST /api/batch      │   │     (PostHog fake)
│  │                       │   │
│  │  GET /received/traces │   │     Returns { count, payloads[] }
│  │  GET /received/metrics│   │     Same
│  │  GET /received/logs   │   │     Same
│  │  GET /received/errors │   │     Sentry-captured ErrorEvents
│  │  GET /received/analytics   │    PostHog-captured batches
│  │                       │   │
│  │  POST /chaos/mode     │   │     { "signal": "traces", "behavior": "503" }
│  │                       │   │
│  └───────────────────────┘   │
│                              │
│  ┌────────────────────────┐  │
│  │ Transport Capture      │  │  ← CapturingMessagePublisher (already exists in tests)
│  │ (InMemory)             │  │     Collects PulseIngested events for assertion
│  └────────────────────────┘  │
└──────────────────────────────┘
```

### 4.2 What the Harness Emits

| Source | Signal | Content |
|---|---|---|
| `Sample.Api GET /weatherforecast` | Traces | ActivitySource span with 5 random weather items, HTTP instrumentation |
| `Sample.Api GET /error` | Traces + Logs | Span with `Status=ERROR`, exception event; error log record |
| `Sample.Api POST /analytics/feature-used` | Analytics | `TelemetryEvent` via `IAnalyticsEmitter.EmitAsync` |
| Direct HTTP/gRPC calls from test runner | All signals | Synthetic OTLP payloads with known span/metric/log counts |

### 4.3 Assertion Strategy (Vendor-SDK-Free)

The harness asserts success **without coupling to Sentry/PostHog/Grafana SDKs**:

1. **FakeSinkReceiver** replaces real sink endpoints. The Collector's sink implementations (Tempo, Loki, Mimir, PostHog) are configured with URLs pointing at the fake receiver.
2. **CapturingMessagePublisher** (already exists in test suite) captures `PulseIngested` events.
3. **Sentry sink** — For integration tests, use `FakeErrorSink` (already exists) implementing `IErrorSink`. For FakeSinkReceiver-based tests, Sentry is the one sink that can't be faked via URL redirect (SDK-based, not HTTP passthrough). Override with `FakeErrorSink` via DI.
4. **PostHog sink** — *Can* be faked via URL redirect to FakeSinkReceiver's `/api/batch` endpoint.
5. **Collector's own self-telemetry** — Verify `CollectorTelemetry` counters via `System.Diagnostics.Metrics.MeterListener` in-process.

### 4.4 Configuration for Demo Harness

```json
{
  "HoneyDrunk": {
    "Pulse": {
      "Collector": {
        "ServiceName": "Pulse.Collector",
        "Environment": "development",
        "EnableTransportPublishing": true,
        "TransportAdapter": "InMemory",
        "EnablePostHogSink": true,
        "EnableSentrySink": true,
        "EnableTempoSink": true,
        "EnableLokiSink": true,
        "EnableMimirSink": true,
        "EnableAzureMonitorSink": false,
        "MaxBatchSize": 1000,
        "ProcessingTimeoutSeconds": 30
      }
    },
    "OpenTelemetry": {
      "OtlpEndpoint": "http://localhost:4317"
    },
    "PostHog": {
      "ApiKeySecretName": "PostHog--ApiKey",
      "Host": "http://localhost:9999",
      "Enabled": true
    },
    "Sentry": {
      "DsnSecretName": "Sentry--Dsn",
      "Environment": "development",
      "Enabled": true
    },
    "Tempo": {
      "Endpoint": "http://localhost:9999/v1/traces"
    },
    "Loki": {
      "Endpoint": "http://localhost:9999/v1/logs",
      "MinimumLogLevel": "Information"
    },
    "Mimir": {
      "Endpoint": "http://localhost:9999/v1/metrics"
    }
  }
}
```

### 4.5 Configuration Notes

- **Loki `MinimumLogLevel`**: Default is `Warning`. Demo harness overrides to `Information` to capture more logs during verification. Production should use default or higher.
- **Secret keys**: `SecretValidationExtensions` looks for `PostHog--ApiKey`, `Sentry--Dsn`, `AzureMonitor--ConnectionString`, and `AzureServiceBus--ConnectionString` via `ISecretStore` (Vault). Sink credentials are never supplied by configuration values.
- **Sink config section names**: All under `HoneyDrunk:` — `Tempo`, `Loki`, `Mimir`, `AzureMonitor`, `PostHog`, `Sentry`.

### 4.6 What the Harness Does NOT Include

- No real Sentry/PostHog/Grafana stack.
- No Docker Compose with real backends (that's post-Phase-1).
- No performance benchmarking tooling (BenchmarkDotNet is post-Phase-1).
- The FakeSinkReceiver is NOT tested itself beyond basic smoke — it's test infrastructure.

---

## 5. Observability of the Observability

How we confirm that the Collector itself is healthy, processing correctly, and routing decisions are visible.

### 5.1 Health Probes

| Probe | Endpoint | What It Confirms |
|---|---|---|
| Liveness | `GET /live` | Process is running, not deadlocked |
| Readiness | `GET /ready` | Pipeline is warmed, sinks initialized |
| Health | `GET /health` | General health (extends to sink connectivity post-Phase-1) |

**Phase 1 Gap:** Health probes return static 200. They do NOT check sink reachability. This is accepted for Phase 1.

### 5.2 Self-Telemetry Metrics

**Meter Name:** `HoneyDrunk.Pulse.Collector.Metrics` (from `TelemetryNames.Meters.PulseCollector`)

| Metric | Type | Unit | What It Reveals |
|---|---|---|---|
| `pulse.collector.traces.ingested` | Counter\<long\> | traces | Total traces received, tagged by `source` |
| `pulse.collector.metrics.ingested` | Counter\<long\> | metrics | Total metrics received |
| `pulse.collector.logs.ingested` | Counter\<long\> | logs | Total logs received |
| `pulse.collector.analytics_events.ingested` | Counter\<long\> | events | Total analytics events received |
| `pulse.collector.errors` | Counter\<long\> | errors | Errors during processing, tagged by `error_type` |
| `pulse.collector.errors.forwarded` | Counter\<long\> | errors | Error spans/logs forwarded to Sentry |
| `pulse.collector.processing.duration` | Histogram\<double\> | ms | Processing duration per ingestion call |

**Verification:** Use `MeterListener` in integration tests to assert counters incremented correctly.

### 5.3 Self-Telemetry Traces

**ActivitySource Name:** `HoneyDrunk.Pulse.Collector` (from `TelemetryNames.ActivitySources.PulseCollector`)

| Activity | Source | What It Reveals |
|---|---|---|
| `ProcessTraces` | `HoneyDrunk.Pulse.Collector` | Trace of each ingestion call, includes error status |
| `ProcessMetrics` | Same | Same |
| `ProcessLogs` | Same | Same |
| `ProcessAnalyticsEvents` | Same | Same |
| `ProcessError` | Same | Same |

**Verification:** Use `ActivityListener` in integration tests to assert activities created with correct names and status codes.

### 5.4 Structured Logging

All logging uses source-generated `LoggerMessage` methods with explicit event IDs:

| EventId Range | Category |
|---|---|
| 1-20 | `IngestionPipeline` — processing, routing, sink failures |
| 200-202 | `TelemetryEnricher` — enrichment decisions |
| 400-406 | `OtlpEndpoints` — request-level errors |

**Verification:** Capture `ILogger` output in tests via `TestLoggerProvider` and assert specific EventIds emitted.

### 5.5 Backlog / Queue Depth

**Phase 1 Status:** No internal queueing. Ingestion is synchronous per-request. There is no internal backlog to monitor.

**Post-Phase-1:** If internal buffering/batching is added, expose `pulse.collector.queue.depth` gauge.

### 5.6 Routing Decision Visibility

Every ingestion publishes a `PulseIngested` Transport event with:
- `Status` (`Success`, `PartialSuccess`, `Failed`)
- `Metadata["pulse.sink_failures"]` — count of sinks that errored
- `ErrorMessage` — human-readable failure summary
- `ProcessingDurationMs` — end-to-end processing time

This event is the **audit trail** for routing decisions.

---

## 6. Failure / Chaos Cases

### 6.1 Sink Unreachable

**Scenario:** Tempo/Loki/Mimir endpoint returns HTTP 503 or connection refused.

| Aspect | Behavior |
|---|---|
| Affected code | `ExportToTraceSinksAsync`, `ExportToLogSinksAsync`, `ExportToMetricsSinksAsync` |
| Isolation | Each sink is called in its own try/catch. One sink failure does NOT prevent other sinks from receiving data. |
| Error reporting | `LogTraceSinkForwardingFailed` (EventId varies by signal). Counter `pulse.collector.errors` incremented. |
| Ingestion result | `PulseIngested.Status = PartialSuccess`. `ErrorMessage = "N sink(s) failed to export"`. |
| Client response | HTTP 200 (accepted) — the Collector acknowledges receipt even if forwarding fails. |
| Retry | Phase 1: No retry. The individual sink HTTP client may retry per `HttpClient` policy (Polly not wired by default). |

**Test:** `SinkFailureScenarioTests` already covers this. FakeSinkReceiver chaos mode extends to integration level.

### 6.2 Malformed OTLP Payload

**Scenario:** Random bytes or invalid JSON sent to OTLP endpoints.

| Aspect | Behavior |
|---|---|
| Affected code | `OtlpParser.ParseTracesAsync` / `ParseMetricsAsync` / `ParseLogsAsync` |
| Protection | Parser wraps all parsing in try/catch. Returns `*.Empty` result (zero counts, no errors). |
| Protobuf fallback | For protobuf, uses byte-length heuristic if proto deserialization fails. |
| JSON fallback | Catches `JsonException`, falls back to byte-length heuristic. |
| Client response | HTTP 200 with `SpanCount=0` (not 400). This is OTLP-compliant — receivers should accept partial data. |
| Logging | `LogTraceParseError` / `LogMetricParseError` / `LogLogParseError` emitted. |

**Test:** `OtlpParserTests.ParseTracesAsync_WithInvalidJson_FallsBackToHeuristic` exists. Add integration test with random bytes.

### 6.3 Oversized Payload / High Volume Burst

**Scenario:** 10 MB payload or 100 concurrent requests.

| Aspect | Behavior |
|---|---|
| Body size | Kestrel default max request body = ~28.6 MB. Payloads under this processed normally. |
| Memory | `MemoryStream` copies full body. For 10 MB, ~20 MB heap allocation per request (original + copy). |
| Concurrency | ASP.NET Core thread pool handles concurrent requests. No internal locking in pipeline. |
| `MaxBatchSize` | Currently not enforced at intake (option exists but not gated). Phase 1 gap. |
| Backpressure | None. If sinks are slow, requests queue in Kestrel. |
| Risk | Sustained burst > 1000 req/s could cause memory pressure. Phase 1 accepts this risk. |

**Test:** Load test with 100 concurrent requests; assert no 500s, no lost data, `processing.duration` histogram populated.

### 6.4 Missing Correlation / Missing NodeId

**Scenario:** No `IOperationContextAccessor.Current`, no `X-Source-*` headers.

| Aspect | Behavior |
|---|---|
| Enrichment | `TelemetryEnricher` checks for null at every level. No NRE. |
| Grid tags | `honeydrunk.node_id`, `honeydrunk.correlation_id` simply absent from enriched attributes. |
| `PulseIngested` | `SourceNodeName = null`, `SourceNodeId = null`, `CorrelationId` from default GridContext (new GUID). |
| Client response | Unchanged — 200 accepted. |

**Test:** `TelemetryEnricherTests.EnrichResourceAttributes_*` covers unit level. Integration test with zero headers needed.

### 6.5 Sink Throttling / Rate Limits

**Scenario:** PostHog returns HTTP 429; Tempo returns 429 with `Retry-After`.

| Aspect | Behavior |
|---|---|
| Tempo/Loki/Mimir | HTTP passthrough. 429 treated as exception by `HttpClient`. Caught by sink try/catch. Logged as failure. `PartialSuccess`. |
| PostHog | `PostHogSink` has built-in retry with exponential backoff for 5xx. **4xx (including 429) does NOT retry** — the code checks `status >= 400 && status < 500` and throws immediately without retry. This means rate-limit responses (429) are treated as terminal failures, not transient. |
| Sentry | SDK handles its own rate limiting internally. |
| Phase 1 gap | No `Retry-After` header parsing. No circuit-breaker. **PostHog 429 not retried is a known behavioral gap** — conventionally, 429 should be retried with backoff using the `Retry-After` header. |

**Test:** FakeSinkReceiver returns 429 for specific signal; assert `PartialSuccess` and error logged.

### 6.6 Sentry SDK Failure (Special Case)

**Scenario:** Sentry is the only sink that uses SDK calls (not HTTP passthrough). If `SentrySdk.CaptureException` throws:

| Aspect | Behavior |
|---|---|
| Error routing | `ProcessErrorAsync` catches exception in its own try/catch. Does NOT rethrow. |
| Error span forwarding | `ForwardErrorSpansToSentryAsync` catches per-span. One span failure doesn't block others. |
| Ingestion | Continues normally. No impact on OTLP response. |

### 6.7 Transport Publishing Failure (Critical Finding)

**Scenario:** `IMessagePublisher.PublishAsync` throws (Transport broker unreachable, serialization failure, etc.).

| Aspect | Behavior |
|---|---|
| Affected code | `PulseIngestedPublisher.PublishAsync` → called by `IngestionPipeline.PublishIngestionEventAsync` |
| Exception handling | **`PulseIngestedPublisher.PublishAsync` does NOT catch exceptions.** Errors propagate to calling `Process*Async` method. |
| Impact on traces/metrics/logs | `ProcessTracesAsync`, `ProcessMetricsAsync`, `ProcessLogsAsync`, `ProcessAnalyticsEventsAsync` all call `PublishIngestionEventAsync` inside their main `try` block **after** sink forwarding. The catch block logs the error and **rethrows via `throw;`**, causing HTTP 500. |
| Impact on errors | `ProcessErrorAsync` is the **only** pipeline method that swallows exceptions. Transport failures during error processing are silently absorbed. |
| Key nuance | **Sinks ARE called before the publish step**, so sink delivery succeeds even when transport fails. But the HTTP response returns 500, which may cause OTEL SDK producers to retry, resulting in duplicate sink deliveries. |

**Risk Assessment:**

This is a **Phase 1 behavioral gap**. The current behavior means:
- A Transport broker outage causes OTLP endpoints to return 500
- OTEL SDK producers retry, leading to duplicate data in sinks
- Only `ProcessErrorAsync` (via `/otlp/v1/errors`) is resilient to this

**Possible remediation (Phase 2):** Wrap `PublishIngestionEventAsync` in its own try/catch within each `Process*Async` method to isolate transport failures from the ingestion response. This would make transport publishing fire-and-forget from the ingestion path.

**Test:**
- `TransportError_Traces_Returns500` — Verify that transport failure causes HTTP 500 for traces
- `TransportError_Errors_Swallowed` — Verify that `ProcessErrorAsync` absorbs transport failure gracefully
- `TransportError_SinksStillCalled` — Verify sinks receive data even when transport subsequently fails

### 6.8 Pipeline Method Exception Behavior Summary

| Method | Sink errors | Transport errors | Rethrows? |
|---|---|---|---|
| `ProcessTracesAsync` | Caught per-sink, counted, `PartialSuccess` | Propagates → HTTP 500 | **Yes** |
| `ProcessMetricsAsync` | Caught per-sink, counted, `PartialSuccess` | Propagates → HTTP 500 | **Yes** |
| `ProcessLogsAsync` | Caught per-sink, counted, `PartialSuccess` | Propagates → HTTP 500 | **Yes** |
| `ProcessAnalyticsEventsAsync` | Caught, counted, `PartialSuccess` | Propagates → HTTP 500 | **Yes** |
| `ProcessErrorAsync` | Caught, logged | Caught, logged | **No** (swallows all) |

---

## 7. Scope Guardrails — Phase 1 Boundaries

### What Pulse.Collector MUST Do

- [x] Accept OTLP HTTP + gRPC for traces, metrics, logs
- [x] Accept custom JSON for analytics and error reports
- [x] Parse OTLP payloads (protobuf and JSON)
- [x] Enrich with Grid metadata (NodeId, Environment, CorrelationId, etc.)
- [x] Route to registered sinks with failure isolation
- [x] Publish `PulseIngested` via Transport
- [x] Fail-fast on invalid configuration in production
- [x] Expose health probes
- [x] Emit self-telemetry (counters, histograms, activities)
- [x] Never leak secrets or PII into logs/telemetry

### What Pulse.Collector MUST NOT Do in Phase 1

| Excluded Capability | Reason |
|---|---|
| Internal batching/buffering | Would require reimplementing OTEL Collector's batch processor. Use synchronous per-request processing. |
| Retry engine with circuit-breaker/backoff | Would require reimplementing OTEL Collector's retry/queue sender. Sink-level retry (if any) is the sink's responsibility. |
| Payload transformation / attribute mutation on pass-through | Raw OTLP bytes forwarded as-is to Tempo/Loki/Mimir. Enrichment applies to `PulseIngested` metadata, not to the forwarded OTLP stream. |
| OTLP Exporter aggregation (delta → cumulative) | Not a collector responsibility. |
| Sampling/tail-sampling | Out of scope — sampling is producer-side via OTEL SDK. |
| Authentication / mTLS on OTLP endpoints | `RequireOtlpAuthentication` option exists but is not enforced in Phase 1. |
| Health probes that check sink connectivity | Static 200s only. Dynamic health requires per-sink health contributors (post-Phase-1). |
| `MaxBatchSize` enforcement at intake | Option exists but is not gated. Risk accepted. |
| Multi-tenant isolation within Collector | Single-tenant for Phase 1. `TenantId` flows through as metadata but no per-tenant routing. |
| gRPC reflection / OTLP service discovery | Not needed for Phase 1. |
| Transport failure isolation from ingestion response | **Known gap**: Transport publish errors propagate to HTTP 500 (except `ProcessErrorAsync`). Remediation: wrap publish in isolated try/catch. Accepted for Phase 1 with explicit awareness. |
| PostHog 429 retry | PostHog sink does not retry on HTTP 429. Conventionally, 429 should be retried with `Retry-After`. Accepted for Phase 1. |

---

## 8. Go/No-Go Criteria

### Go — Pulse.Collector is Verified When:

| # | Criterion | Evidence Required |
|---|---|---|
| 1 | **Build clean** | `dotnet build -c Release` — zero errors, zero warnings |
| 2 | **All existing tests pass** | `dotnet test` — 128 tests green, zero failures, zero skipped |
| 3 | **HTTP OTLP intake works for all 3 signals** | Smoke tests POST protobuf to `/otlp/v1/traces`, `/metrics`, `/logs` — all return 200 with correct counts |
| 4 | **gRPC OTLP intake works** | gRPC client sends `ExportTraceServiceRequest` — receives OK response with correct counts |
| 5 | **Grid enrichment applied** | Integration test verifies `service.name`, `honeydrunk.environment`, `pulse.ingested_at` present in enriched output |
| 6 | **Sink routing verified per signal** | FakeSinkReceiver or mock sinks confirm: Tempo gets traces, Loki gets logs, Mimir gets metrics, PostHog gets analytics, Sentry gets errors |
| 7 | **Sink failure isolation proven** | One sink failure → `PartialSuccess`; other sinks still receive data; no 500 to client |
| 8 | **Transport integration works** | `PulseIngested` event published with correct metadata, counts, and Grid context |
| 9 | **Fail-fast config validation works** | Production with missing OTLP / localhost / InMemory transport → startup throw confirmed |
| 10 | **No PII / secret leakage** | Log output and telemetry attributes audited — zero secret values found |
| 11 | **Malformed input handled gracefully** | Random bytes / invalid JSON → 200 with zero counts, no crash |
| 12 | **Self-telemetry functional** | `MeterListener` or log capture confirms counters increment on ingestion |
| 13 | **Health probes respond** | `/health`, `/ready`, `/live` all return 200 |
| 14 | **Sample.Api → Collector → FakeSink end-to-end** | Full pipeline from OTEL SDK producer through Collector to deterministic fake sink, all assertions pass |

### No-Go — Block If Any Of:

| # | Blocker | Impact |
|---|---|---|
| 1 | Build has errors or analyzer violations | Standards non-compliance |
| 2 | Any existing test regresses | Trust in verification impossible |
| 3 | **Sink** failure causes 500 to client or crashes pipeline | Reliability invariant violated (note: **Transport** publish failures currently DO cause 500 — this is a documented Phase 1 gap, not a blocker unless isolated before verification) |
| 4 | Secrets appear in structured logs or telemetry tags | Security invariant violated |
| 5 | Grid metadata not enriched (missing `service.name` default, missing `pulse.ingested_at`) | Contract with downstream consumers broken |
| 6 | `PulseIngested` Transport event missing or empty | Grid audit trail broken |
| 7 | Production starts with localhost OTLP or InMemory transport without failing | Fail-fast safety net broken |

---

## Appendix A: Existing Test Coverage Summary

| Test Class | Count | Covers |
|---|---|---|
| `CollectorSmokeTests` | 9 | Endpoint reachability, basic HTTP acceptance |
| `IngestionPipelineTests` | 14 | Pipeline routing logic, enrichment, Transport publishing |
| `OtlpParserTests` | 11 | JSON/protobuf parsing accuracy, error extraction |
| `OtlpEndpointValidationTests` | 9 | Fail-fast config validation |
| `SinkFailureScenarioTests` | 12 | Multi-sink failure isolation |
| `TelemetryEnricherTests` | 11 | Attribute enrichment logic |
| `TransportValidationTests` | 13 | Transport adapter validation |
| `PostHogMappingTests` | 10 | Event → PostHog payload mapping |
| `PostHogSinkTests` | 9 | PostHog HTTP batching, retry, disposal |
| `SentrySinkTests` | 8 | Sentry options validation, ErrorEvent construction |
| `TelemetryEventTests` | 14 | TelemetryEvent model, fluent API |
| `PulseIngestedPublishTests` | 4 | PulseIngested contract, IngestionStatus |
| `TransportPublishingIntegrationTests` | 4 | End-to-end Transport publishing with `CapturingMessagePublisher` |
| **Total** | **128** | |

### Coverage Gaps Identified for Phase 1 Verification

| Gap | Priority | Test Type |
|---|---|---|
| gRPC intake end-to-end | High | Integration — gRPC client → Collector |
| Multiple sinks per signal type (e.g., Tempo + AzureMonitor both receive traces) | High | Integration — DI registers both sinks |
| FakeSinkReceiver-based end-to-end (HTTP passthrough verification) | High | Integration — Collector → real HTTP → fake |
| MeterListener-based self-telemetry assertion | Medium | Integration — verify counters |
| PII audit (grep for secret patterns in logs) | Medium | Integration — structured log capture |
| Chaos: 429 / slow sink / timeout | Medium | Integration via FakeSinkReceiver chaos mode |
| Oversized payload resilience | Low | Load — 10 MB body |
| High-concurrency burst (100+ parallel) | Low | Load — parallel HTTP clients |
| `MaxBatchSize` enforcement verification (currently NOT gated) | Low | Document as known gap |
| Transport error isolation from ingestion path | **High** | Integration — `IMessagePublisher` throws; assert sinks called, verify HTTP response behavior |
| PostHog 429 retry behavior | Medium | Unit — PostHogSink returns 429; assert NOT retried (document gap) |
| `ProcessErrorAsync` exception swallowing verification | Medium | Unit — pipeline method absorbs all exceptions |
| Loki default `MinimumLogLevel` (Warning) documentation | Low | Config documentation accuracy |

---

## Appendix B: Pseudocode — FakeSinkReceiver

```pseudocode
class FakeSinkReceiver:
    received = ConcurrentDictionary<string, ConcurrentBag<ReceivedPayload>>()
    chaosMode = ConcurrentDictionary<string, ChaosBehavior>()

    // OTLP passthrough endpoints
    POST /v1/traces:
        if chaosMode["traces"] == "503": return 503
        if chaosMode["traces"] == "429": return 429, Retry-After: 5
        if chaosMode["traces"] == "slow": await Task.Delay(10_000)
        body = await ReadBodyAsync()
        received["traces"].Add(new ReceivedPayload(body, headers, timestamp))
        return 200

    POST /v1/metrics:
        // same pattern

    POST /v1/logs:
        // same pattern

    POST /api/batch:
        // PostHog fake — accept JSON batch
        body = await ReadBodyAsync()
        received["analytics"].Add(new ReceivedPayload(body, headers, timestamp))
        return 200

    // Assertion endpoints
    GET /received/{signal}:
        return { count: received[signal].Count, payloads: received[signal].ToList() }

    GET /received/{signal}/latest:
        return received[signal].Last()

    DELETE /received:
        received.Clear()
        return 204

    // Chaos control
    POST /chaos/mode:
        { signal, behavior } = ParseBody()
        chaosMode[signal] = behavior
        return 200

    DELETE /chaos/mode:
        chaosMode.Clear()
        return 200

record ReceivedPayload(byte[] Body, Dictionary<string, string> Headers, DateTimeOffset ReceivedAt)
enum ChaosBehavior { None, Return503, Return429, SlowResponse, Timeout }
```

---

## Appendix C: Pseudocode — Integration Test Skeleton

```pseudocode
class PulseCollectorIntegrationTests:

    // Fixture: starts Collector + FakeSinkReceiver + configures DI overrides
    fixture:
        fakeSink = new FakeSinkReceiver()        // listens on :9999
        collector = WebApplicationFactory<Program>
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    // Override Sentry with FakeErrorSink
                    services.Replace(ServiceDescriptor.Singleton<IErrorSink>(new FakeErrorSink()))
                    // Keep real Tempo/Loki/Mimir sinks → they HTTP to FakeSinkReceiver
                ))
            .CreateClient()

    test Traces_EndToEnd_ReachsFakeSink:
        // Arrange
        payload = BuildOtlpTraceProtobuf(spanCount: 3, withErrorSpan: true)

        // Act
        response = await collector.PostAsync("/otlp/v1/traces", payload, "application/x-protobuf",
            headers: { "X-Source-Service": "test-api", "X-Source-NodeId": "node-1" })

        // Assert — HTTP response
        assert response.StatusCode == 200
        body = await response.Content.ReadAsAsync()
        assert body.SpanCount == 3
        assert body.ErrorCount == 1

        // Assert — Tempo received raw OTLP
        tempoReceived = await fakeSink.GetAsync("/received/traces")
        assert tempoReceived.Count == 1
        assert tempoReceived[0].Body.Length == payload.Length

        // Assert — Sentry received error event
        assert fakeErrorSink.CapturedEvents.Count == 1
        assert fakeErrorSink.CapturedEvents[0].Tags["trace.id"] != null

        // Assert — Transport event published
        assert capturingPublisher.Events.Count == 1
        event = capturingPublisher.Events[0]
        assert event.TraceCount == 3
        assert event.SourceNodeName == "test-api"
        assert event.Status == IngestionStatus.Success
        assert event.Metadata["honeydrunk.environment"] == "development"

    test SinkUnreachable_PartialSuccess:
        // Arrange
        await fakeSink.PostAsync("/chaos/mode", { signal: "traces", behavior: "503" })
        payload = BuildOtlpTraceProtobuf(spanCount: 2)

        // Act
        response = await collector.PostAsync("/otlp/v1/traces", payload)

        // Assert
        assert response.StatusCode == 200  // Collector always accepts
        event = capturingPublisher.Events.Last()
        assert event.Status == IngestionStatus.PartialSuccess
        assert event.ErrorMessage.Contains("sink(s) failed")

        // Cleanup
        await fakeSink.DeleteAsync("/chaos/mode")
```

---

## Appendix D: Custom Endpoint Request/Response Contracts

### Analytics Events — `POST /otlp/v1/analytics`

**Request (`AnalyticsEventsRequest`):**
```json
{
  "sourceService": "my-api",
  "sourceNodeId": "node-1",
  "events": [
    {
      "eventName": "feature-used",
      "timestamp": "2026-02-21T12:00:00Z",
      "distinctId": "user-123",
      "userId": "user-123",
      "sessionId": "sess-abc",
      "correlationId": "corr-xyz",
      "nodeId": "node-1",
      "environment": "production",
      "properties": {
        "feature": "dark-mode",
        "duration_ms": 150
      }
    }
  ]
}
```

**Response (200):** `{ "status": "accepted", "count": 1 }`  
**Response (400):** `{ "error": "No events provided" }` — when `events` is null or empty  
**Response (400):** `{ "error": "Invalid JSON format" }` — when body is not valid JSON

### Error Report — `POST /otlp/v1/errors`

**Request (`ErrorReportRequest`):**
```json
{
  "message": "NullReferenceException in OrderService",
  "severity": 3,
  "stackTrace": "at OrderService.Process() in ...",
  "correlationId": "corr-xyz",
  "operationId": "op-123",
  "nodeId": "node-1",
  "userId": "user-123",
  "environment": "production",
  "tags": { "endpoint": "/api/orders", "http.method": "POST" },
  "extra": { "order_id": "ORD-9999" }
}
```

**Severity values:** `Debug=0`, `Info=1`, `Warning=2`, `Error=3`, `Fatal=4`  
**Response (200):** `{ "status": "accepted" }`  
**Response (400):** `{ "error": "Invalid request" }` — when body is null  
**Response (400):** `{ "error": "Invalid JSON format" }` — when body is not valid JSON

---

## Appendix E: Secret Validation Keys Reference

| Secret Key | Required When | Validated By |
|---|---|---|
| `PostHog--ApiKey` | `EnablePostHogSink = true` | `SecretValidationExtensions` |
| `Sentry--Dsn` | `EnableSentrySink = true` | `SecretValidationExtensions` |
| `Loki--BasicAuth` | Optional Loki authentication | `LokiSink` via `ISecretStore` |
| `Tempo--BasicAuth` | Optional Tempo authentication | `TempoSink` via `ISecretStore` |
| `Mimir--BasicAuth` | Optional Mimir authentication | `MimirSink` via `ISecretStore` |
| `AzureMonitor--ConnectionString` | `EnableAzureMonitorSink = true` | `SecretValidationExtensions` |
| `AzureServiceBus--ConnectionString` | `TransportAdapter = "AzureServiceBus"` | `SecretValidationExtensions` (secondary) + `TransportValidationExtensions` (primary) |

In non-Development environments, missing secrets cause `InvalidOperationException` (fail-fast).  
In Development, missing secrets emit a warning log.

---

*End of verification plan. Execute checklist sections A through H in order. All Go criteria must pass before declaring Pulse.Collector verified.*
