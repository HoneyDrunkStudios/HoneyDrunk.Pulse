# Claude Code — HoneyDrunk.Pulse

You are operating inside `HoneyDrunk.Pulse`, the Grid's **observability engine**: telemetry
abstractions, OpenTelemetry wiring, the multi-backend sink pipeline, and the Pulse Collector
OTLP receiver.

## Read This First

**The canonical engineering guide for this repo is
[`.github/copilot-instructions.md`](.github/copilot-instructions.md).** It is the single
source of truth for stack, coding standards, boundaries, build/test, deployment, and commit
conventions. Read it before making changes. This file only adds Claude-surface context.

## Non-Negotiables (summary — full detail in the canonical guide)

- **Pulse builds on OTel, doesn't replace it**; it routes, it doesn't store or dashboard.
- **Per-sink failure isolation is an invariant** — one backend outage never affects another
  sink or the OTLP response.
- **Tenant cardinality (ADR-0026):** `tenant_id` stays bounded/low-cardinality; never put
  user/session/request IDs or malformed/internal tenant values into metric labels.
- **Reuse before adding:** the six sinks are deliberately parallel — a new sink extends the
  shared Extensions/Implementation/Options shape; cross-sink logic goes in a shared location,
  not copied per sink. DRY/SOLID. Justify intentional duplication in a comment.
- **Don't change `Telemetry.Abstractions` or `Pulse.Contracts`** without explicit
  instruction (Grid-wide breaking; Contracts multi-targets on purpose).
- **Conventional commits** (`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`,
  `ci:`, `build:`), present tense, ≤ 50-char first line.

## Your Role (planning / hands-on surface)

- Plan and decompose before large edits; prefer the smallest change that satisfies intent.
- When a task needs an architectural decision not covered by an ADR or the issue, stop and
  flag it rather than guessing.
- Tests accompany code changes. Run `dotnet build -c Release` + `dotnet test` before
  declaring done; report failures with output.
- ADR-0015: the deployable is **Pulse.Collector** (`collector-v*`). The `PostHog:Host`
  App Configuration gap is a known, scoped follow-up — don't silently fold it into unrelated
  work.
