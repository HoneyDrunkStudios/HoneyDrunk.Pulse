# Agents — HoneyDrunk.Pulse

This file is for every coding agent (Codex, Claude Code and others) working in
`HoneyDrunk.Pulse`, the Grid's **observability engine**: telemetry abstractions,
OpenTelemetry wiring, the multi-backend sink pipeline, and the Pulse Collector OTLP receiver.
It is the single agent instruction file; there is no separate `CLAUDE.md`.

## Read This First

**The canonical engineering guide is
[engineering guide](docs/engineering-guide.md)** — the maintained reference for stack, coding standards, boundaries, build/test, deployment, and commit
conventions. Read it before implementing. This file only states agent-execution rules.

## Execution Rules

1. Read the selected request or issue: purpose, acceptance criteria, constraints and actual dependencies. No issue or packet is required.
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
5. Add or update tests in `Pulse.Tests` (xUnit + AwesomeAssertions) for changed behavior; documentation-only edits need content/link checks.
6. From `HoneyDrunk.Pulse/`, run `dotnet build -c Release` and `dotnet test -c Release` for code changes. Analyzer compliance
   (`HoneyDrunk.Standards`) is mandatory; warnings are errors.
7. Open a PR aligned to the acceptance criteria.

## Interactive Sessions

When working hands-on with a person rather than executing a scoped issue:

- Plan and decompose before large edits.
- Report build and test failures with their output.
- Pulse builds on OpenTelemetry and does not replace it; it routes telemetry and does not
  store or dashboard it.
- ADR-0015: the deployable is **Pulse.Collector** (`collector-v*`). The `PostHog:Host`
  App Configuration gap is a known, scoped follow-up; do not fold it into unrelated work.

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

## Shared conventions and delivery

Read the [shared engineering conventions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md) and this repository's owning documentation before editing. Apply the parts relevant to this stack; preserve existing public contracts, dependency direction and repository-specific behavior. Verify shared capabilities in current code before reusing them; a catalog entry or scaffold is not an implemented integration.

Work within the selected request. Preserve unrelated changes and use a separate worktree when needed. Review the final diff, use Conventional Commits and ready-for-review PRs with exactly one accurate `Authorship:` line and a `Request:` line; include the authorship in commit trailers. Run meaningful checks for the affected behavior and report the reviewed/tested revision, failures and unrun checks. For documentation-only changes, check links, paths and instruction consistency. Preserve required checks and inspect actual latest-head Sonar new-code findings where analysis applies; do not suppress findings or weaken gates to obtain a pass. Legacy Grid Review is retired; do not restore its workers, queues or bypass labels. A configured replacement reviewer is not evidence of a completed review or enforcing merge check.

## Code Review Rules

Apply the [shared review criteria](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md#code-review) to changed behavior, using the repository boundaries above. Report actionable findings with the failing path, concrete impact and a small corrective action; disclose unavailable evidence. These rules grant no cross-repository access or merge authority.

- Keep telemetry contracts and OpenTelemetry composition distinct from backend-specific sinks; preserve supported contract targets and reuse shared enrichment/dispatch behavior. Pulse routes telemetry and does not become its storage or dashboard owner.
- Trace cancellation, queue pressure and backend failure through each sink: one failed sink must not break another or the OTLP response. Flag unbounded buffers/retries, sensitive payloads, broken correlation and high-cardinality metric labels with a concrete path.
- Require focused contract, sink-isolation, backpressure and failure-path tests for changed behavior. Distinguish in-memory or collector health evidence from actual sink delivery; follow the existing engineering guide and test stack.
