# HoneyDrunk.Pulse Repository Guidelines

## Project Overview

This repository is the **observability engine** for the HoneyDrunk Grid ("the Hive"):
telemetry abstractions, OpenTelemetry integration, a multi-backend sink pipeline, and the
**Pulse Collector** — an OTLP receiver (HTTP + gRPC) that ingests, enriches, and routes
telemetry for every Grid Node.

**Pulse builds on OpenTelemetry, not instead of it.** Pulse is not a storage engine and not
a dashboarding tool — it routes signals to backends (Tempo, Loki, Mimir, Sentry, PostHog,
Azure Monitor) with per-sink failure isolation.

This is a **.NET 10.0** solution composed of (selected projects):

- `HoneyDrunk.Pulse.Contracts` — shared Transport event contracts (multi-target:
  netstandard2.0 + net8.0 + net10.0); `PulseIngested` lives here
- `HoneyDrunk.Telemetry.Abstractions` — sink interfaces (`ITraceSink`, `ILogSink`,
  `IMetricsSink`, `IAnalyticsSink`, `IErrorSink`) and telemetry models
- `HoneyDrunk.Telemetry.OpenTelemetry` — OTel wiring + Grid context enrichment for Nodes
- `HoneyDrunk.Telemetry.Sink.*` — Tempo, Loki, Mimir, PostHog, Sentry, AzureMonitor
- `Pulse.Collector` — OTLP receiver deployable (release line `collector-v*`)
- `HoneyDrunk.Pulse.Sample.Api` / `.Sample.Worker` — instrumentation samples
- `Pulse.Tests` — xUnit test suite

**Version:** see project `<Version>` / `README.md`. The Collector versions on its own tag
line per ADR-0015 (`collector-v*`).

---

## Technology Stack

- **Framework:** .NET 10.0 (Contracts multi-targets for downstream consumers)
- **Language:** C# (`LangVersion` latest)
- **Project Types:** Class libraries + Collector service + samples + xUnit test project
- **Features Enabled:** Implicit Usings, Nullable Reference Types, primary constructors,
  `GenerateDocumentationFile`
- **Standards:** `HoneyDrunk.Standards` analyzers (buildTransitive) — compliance is
  mandatory; warnings are treated as errors

---

## Coding Standards

### C# Conventions

- Follow Microsoft C# conventions plus **HoneyDrunk.Standards** analyzers.
- Nullable enabled everywhere; avoid `!` suppression unless justified with a comment.
- Favor **primary constructors** and immutable, constructor-injected `readonly` dependencies.
- **PascalCase** for public types/members; **camelCase** for locals/parameters.
- Records drop the `I` prefix; interfaces keep it (`TelemetryBatch` record vs `ITraceSink`
  interface) — Grid-wide naming rule.

### Reuse Before You Add (DRY / SOLID)

This is a hard expectation, not a nicety:

- **Before adding a new helper, mapper, enricher, options class, factory, extension method,
  or sink, scan the current type, sibling types, and shared locations** (`Enrichment/`,
  `Extensions/`, `Mapping/`, the sink base/options patterns) for existing behavior to reuse
  or extend.
- The six sinks are deliberately parallel. **A new sink follows the existing
  Extensions/Implementation/Options shape** — extend the shared pattern, don't invent a
  one-off structure. Cross-sink behavior (retry/backoff, OTLP mapping, enrichment) belongs
  in a shared location, not copied into each sink.
- Prefer expanding an existing method over a near-duplicate one-off. Parameterize or compose
  rather than fork. If behavior must genuinely diverge, duplicate intentionally and say why.
- Apply SOLID: single responsibility per type, depend on `Abstractions` not implementations,
  keep sinks substitutable behind their interfaces, keep failure isolation per sink.

### Code Organization

- No `/src` or `/tests` folders. Projects live at repo root under `HoneyDrunk.Pulse/`.
- Each sink: `Extensions/` (DI), `Implementation/`, `Options/`. Keep that shape.
- Collector domains: `Endpoints/`, `Ingestion/`, `Enrichment/`, `Transport/`, `Services/`,
  `Configuration/`, `Protos/`. Keep the deployable thin — shared logic in libraries.

### Documentation

- XML docs required for public APIs in `Abstractions` and `Contracts`.
- Keep `README.md`, `CHANGELOG.md`, and `docs/examples/` current when contracts change.

---

## Boundaries & Discipline

- **Do not change `HoneyDrunk.Telemetry.Abstractions` or `HoneyDrunk.Pulse.Contracts`
  without explicit instruction** — consumed Grid-wide; changes are breaking. Contracts
  multi-target on purpose; preserve the target frameworks.
- **Per-sink failure isolation is an invariant.** One backend's outage must never affect
  another sink or the OTLP HTTP response. Transport publish failures are isolated too.
- **Tenant cardinality (ADR-0026):** `tenant_id` is a bounded, low-cardinality dimension.
  Never emit user/session/request identifiers or malformed/internal tenant values as metric
  labels. Respect the cardinality kill criteria.
- Grid context (correlation, Node/Studio/Environment) flows via **Kernel** primitives and
  the `TelemetryEnricher` — don't invent a parallel enrichment path.
- Secrets (backend tokens, PostHog host) resolve via **Vault** / App Configuration — never
  hardcode.

---

## Build and Testing

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
```

- Targets **.NET 10.0**. Warnings are errors.
- Tests live only in `Pulse.Tests` — no test code in libraries or the Collector. Prefer
  **xUnit** + **AwesomeAssertions**. Test classes mirror implementation.
- All code changes include tests unless the issue explicitly says otherwise.

---

## Deployment (ADR-0015 — Azure Container Apps)

- Deployable: **Pulse.Collector**, tag line `collector-v*` (e.g. `collector-v0.1.0`).
- Release workflow: `.github/workflows/release-collector.yml` (tag → build → ACR push →
  revision at 0% → health probe → traffic shift).
- The real Collector image serves OTLP on `:8080`; Container Apps ingress reconciles to
  Transport HTTP/2 / port 8080 after the first successful deploy.
- **Config gap:** `HoneyDrunk:PostHog:Host` must be set to `https://us.i.posthog.com` in
  App Configuration (Pulse label) or analytics events silently drop despite a green deploy.
- Never commit environment-specific IDs, backend tokens, or secrets.

---

## Commit & Contribution Conventions

- **Conventional commits, always:** `feat:`, `fix:`, `chore:`, `docs:`, `test:`,
  `refactor:`, `ci:`, `build:`. Use a scope when it clarifies
  (`feat(sink.loki):`, `fix(collector):`). Present tense, concise first line (≤ 50 chars).
- Breaking contract changes: note `BREAKING CHANGE:` in the commit body.
- Keep PRs small and focused; align with the issue's acceptance criteria.
- Run build + tests locally before pushing. Analyzer compliance is mandatory.
- Respect `.gitignore` / `.gitleaks.toml` / `.trivyignore` — never commit `bin/`, `obj/`,
  or secrets.
