# Agents — HoneyDrunk.Pulse

This file is for autonomous coding agents (Codex and other non-IDE agents) executing scoped
tasks in `HoneyDrunk.Pulse`, the Grid's **observability engine**.

## Read This First

**The canonical engineering guide is
[`.github/copilot-instructions.md`](.github/copilot-instructions.md)** — the single source of
truth for stack, coding standards, boundaries, build/test, deployment, and commit
conventions. Read it before implementing. This file only states agent-execution rules.

## Execution Rules

1. Read the issue: task, acceptance criteria, constraints, dependencies.
2. Confirm the work belongs in Pulse (telemetry routing/enrichment/ingestion) and respects
   per-sink failure isolation and tenant-cardinality discipline (ADR-0026).
3. Implement the smallest change that satisfies the acceptance criteria.
4. **Reuse before adding.** Before adding a new helper, mapper, enricher, options class,
   factory, extension method, or sink, scan the current type, sibling types, and shared
   locations for existing behavior to reuse or extend. The six sinks are deliberately
   parallel — a new sink follows the existing Extensions/Implementation/Options shape;
   cross-sink behavior goes in a shared location, not copied per sink. Prefer cohesive
   shared methods over one-off near-duplicates; justify intentional duplication in a
   comment. DRY/SOLID.
5. Add or update tests in `Pulse.Tests` (xUnit + FluentAssertions) unless the issue says
   otherwise.
6. Run `dotnet build -c Release` and `dotnet test -c Release` locally. Analyzer compliance
   (`HoneyDrunk.Standards`) is mandatory; warnings are errors.
7. Open a PR aligned to the acceptance criteria.

## Do Not

- Do not change `HoneyDrunk.Telemetry.Abstractions` or `HoneyDrunk.Pulse.Contracts` without
  explicit instruction — Grid-wide breaking; preserve Contracts' multi-target frameworks.
- Do not break per-sink failure isolation — one backend's failure must not affect another
  sink or the OTLP HTTP response.
- Do not emit user/session/request identifiers or malformed/internal tenant values as metric
  labels (ADR-0026 cardinality discipline).
- Do not make architectural decisions not covered by the issue or a governing ADR — flag it.
- Do not commit secrets, backend tokens, environment-specific IDs, `bin/`, or `obj/`
  (respect `.gitignore` / `.gitleaks.toml` / `.trivyignore`).

## Commits

Conventional commits only: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`,
`ci:`, `build:` — optional scope (`fix(sink.posthog):`), present tense, ≤ 50-char first
line, `BREAKING CHANGE:` in the body when a public contract changes.
