# HoneyDrunk.Pulse.Sample.Api

A minimal ASP.NET Core sample that demonstrates HoneyDrunk telemetry in a web API.
It wires up `AddHoneyDrunkOpenTelemetry` (traces, metrics, logs) and the Pulse
analytics emitter, then exposes endpoints that exercise each path:

- `GET /weatherforecast` — a traced success endpoint that tags the active activity.
- `GET /error` — throws to show error/exception tracking.
- `POST /analytics/feature-used` — emits a `TelemetryEvent` via `IAnalyticsEmitter` to the Pulse Collector.
- `GET /health` — liveness probe.

By default it ships traces to an OTLP endpoint at `http://localhost:4317` and
analytics to a Pulse Collector at `http://localhost:5000`; override via
`HoneyDrunk:OpenTelemetry:OtlpEndpoint` and `HoneyDrunk:Pulse:CollectorEndpoint`
in configuration.

## How to run

```bash
dotnet run --project HoneyDrunk.Pulse/HoneyDrunk.Pulse.Sample.Api/HoneyDrunk.Pulse.Sample.Api.csproj
```

Then send requests (see `HoneyDrunk.Pulse.Sample.Api.http`), e.g.:

```bash
curl http://localhost:5xxx/weatherforecast
```
