# Telemetry safety and operations

This is an operator runbook and a recommended starting policy, not evidence of a
live deployment or retention setting. Pulse routes telemetry; each destination
owns storage, access controls, retention, quotas, and alerts.

## Collection and redaction boundaries

- The OpenTelemetry integration applies the shared `TelemetryRedactor` before its
  trace and log exporters. The analytics emitter sanitizes copies before sending.
- Collector fan-out parses and sanitizes OTLP JSON and protobuf once per signal
  batch before any trace, log, or metric sink receives it. Resource, scope, span,
  event, link, log-body, metric-point, and exemplar attributes are inspected.
- Sensitive named fields (including punctuation/case variants of credentials,
  authorization, cookies, connection strings, email and phone) are replaced.
  Common labeled credentials, bearer/basic values, URL credentials, and email
  addresses in free text are also replaced. Arbitrary unlabeled secrets and all
  forms of personal information cannot be recognized reliably. Never record
  bodies, credentials, personal content, or authentication headers at the source.
- Binary OTLP attribute/body values and unknown protobuf fields are removed or
  replaced because they cannot be inspected safely. This intentionally stops
  opaque pass-through; update the bundled schema before adopting new OTLP fields.
- Malformed, unsupported, or excessively nested OTLP cannot fall back to raw
  export. Redaction is not a retry or durable storage mechanism.
- Custom error events, extracted error spans/logs, and analytics properties use the
  same policy before sink calls. Raw exception objects are not forwarded. Collector
  callers passing an in-process `Exception` through the pipeline receive a sanitized
  native Sentry exception type/value plus sanitized structured frames (at most 128,
  oldest-first) for useful grouping. Raw exception Data and inner-exception objects
  are withheld. Text-only OTLP stack traces remain sanitized diagnostic text; native
  frames are not invented. Group hashes can differ from prior SDK formatting, so
  validate existing error grouping during rollout.
  Collector ingestion-pipeline, parser, and endpoint failure logs use exception type
  and aggregate counts rather than payload content or sink exception messages. Failed sink calls remain isolated from other sinks.
- SDK scope collection is disabled: the public logging API cannot safely replace
  all captured scope values. Host console/file providers are outside the OTel
  export processor; callers must sanitize their values before logging to them.
- A caller that directly invokes a sink bypasses the collector boundary. Such
  callers must sanitize their data first or send through the collector. Adding
  independent providers/exporters or resource detectors also requires reviewing
  their data before export; the built-in policy is not a process-wide interceptor.

Maintain tests with unmistakable sentinel values in structured attributes, free
text, nested objects, exceptions, and both OTLP encodings. Assert the sentinel is
absent from every captured exporter/sink/log, and that valid W3C trace/span IDs,
counts, timestamps, and non-sensitive measurements survive. Never use real secrets
or personal data in fixtures. A redaction failure is an ingestion failure; do not
add a raw-data fallback to improve availability.

## Ingestion access and limits

See [collector authentication](../Pulse.Collector/README.md#ingestion-authentication)
for supported JWT authority/audience configuration and the deliberate production
migration. Configure clients through standard `OTEL_EXPORTER_OTLP_HEADERS` (or the
signal-specific equivalent) using a short-lived bearer access token from the
approved issuer. Do not put token values in source, logs, command history, or this
runbook. Client identity/token acquisition and rotation remain the host's responsibility.

Only allow authorized producers to reach the ingestion endpoint. The JWT audience
must be dedicated to ingestion; an audience shared with an unrelated API is not
an authorization boundary. If an authenticated gateway is used with the explicit
anonymous-backend opt-out, deny every direct route to the collector and verify
both HTTP and HTTP/2 gRPC requests pass through that gateway. A request header that
claims an identity or tenant is not authentication or tenant authorization.

Configure upstream body-size, rate, concurrency, and timeout limits appropriate
to expected batches. `MaxBatchSize` and `ProcessingTimeoutSeconds` are currently
options, not enforced admission limits. Do not rely on them for denial-of-service
protection. Test an oversized batch and sustained excess traffic at the actual
edge before production rollout. Keep health endpoints free of payload data and
restricted by network policy where appropriate.

## Trace context and metric labels

Use .NET `Activity` and OpenTelemetry's ASP.NET Core/HttpClient instrumentation for
W3C `traceparent`/`tracestate`. Use the standard propagator across asynchronous
carriers and start consumer activities from the extracted parent context. Never
convert a business correlation ID, user ID, or job ID into a W3C trace/span ID.
Keep business correlation fields as separate attributes. Test cross-boundary
parent trace ID, span parent, sampling flags, and tracestate with malformed-header
coverage. Do not place credentials or personal data in baggage or tracestate.

Metric names and labels must describe bounded dimensions. Never use user/session/
request IDs, trace/span IDs, email, raw URLs, exception messages, or arbitrary
payload values as metric dimensions. Use route templates, finite operation names,
status classes, and an approved service roster. Correlate an individual operation
with traces, logs, or exemplars instead.

Collector tenant labels currently reject malformed/internal values, but syntactic
validation does not bound the number of real tenant IDs. The `source.name` label
also comes from producer input. Require trusted producers, monitor distinct label
counts, and configure standard OpenTelemetry Views at the producer to allow only
approved keys and set an explicit per-instrument cardinality limit. Set a backend
series quota as a second limit. Review any tenant/service roster growth before
raising these limits. Do not silently strip labels after aggregation: two distinct
series may collide and change metric meaning. Attribute redaction is a safety net,
not an aggregation or cardinality policy.

## Retention and access review

Before rollout, record a named operational owner, destination, purpose, data
classification, retention, deletion procedure, access group, and cost budget for
each signal. Suggested initial maximums, subject to the data owner's approval:

| Signal | Starting retention | Notes |
| --- | --- | --- |
| Diagnostic logs | 14 days | Avoid payloads; shorter for elevated verbosity |
| Traces | 7 days | Sample normal traffic; preserve useful errors within budget |
| Error events | 30 days | Review attached context and user identifiers |
| Aggregated metrics | 90 days | Bounded dimensions; no personal identifiers |
| Product analytics | 30 days | Purpose-specific approval and deletion process |

These are proposed values, not configured defaults or legal advice. Implement the
approved values in each existing destination, including replicas, exports, and
backups where supported. Verify expiry with an old synthetic record and evidence
from the destination. Review access quarterly and whenever personnel or data
purpose changes. Keep operational telemetry separate from audit records that have
independent retention obligations.

## Alerts and verification

Start with service objectives and tune against a normal-traffic baseline. Suggested
rules below need deployment-specific thresholds and an on-call owner before use:

| Condition | Starting response |
| --- | --- |
| Collector readiness fails or expected telemetry stops for 5 minutes | Page the service owner; check reachability and exporter health |
| `pulse.collector.errors` stays above baseline for 5 minutes | Inspect error type, destination availability, and recent configuration changes |
| Sink failures or partial-success ingestion events appear | Notify the destination owner; verify every enabled sink independently |
| Processing-duration p95 exceeds the agreed latency budget for 10 minutes | Check backlog, sink latency, and load; enforce admission limits |
| Authentication failures spike | Check token expiry/audience and unauthorized producers; never log tokens |
| Active series or daily ingest cost approaches 80% of budget | Investigate new label values or verbose sources before raising quotas |

An accepted HTTP/gRPC request and healthy probe do not prove durable delivery.
Pulse has no durable per-sink queue here. Trace/log/metric fan-out can acknowledge
partial success while one destination is unavailable. Ingestion events carry
`PartialSuccess` and `pulse.sink_failures` for those fan-out failures when Transport
publishing is enabled. Error-sink forwarding and Transport-publish failures have
separate logs/metrics and are not all included in that count. Combine these signals
with destination-side receipts and a synthetic canary; do not alert only on HTTP 5xx.

For each environment, send a non-sensitive canary with a known trace ID, confirm
arrival in every enabled destination, verify redaction, test missing/invalid/expired
JWTs over HTTP and gRPC, and simulate one sink outage. Record who receives each
alert and the expected recovery steps. Use destination retry/backpressure features
where appropriate; do not assume Pulse can replay a previously accepted batch.

References: [OpenTelemetry sensitive-data guidance](https://opentelemetry.io/docs/security/handling-sensitive-data/),
[.NET metric best practices](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices/),
[.NET tracing best practices](https://opentelemetry.io/docs/languages/dotnet/traces/best-practices/).
