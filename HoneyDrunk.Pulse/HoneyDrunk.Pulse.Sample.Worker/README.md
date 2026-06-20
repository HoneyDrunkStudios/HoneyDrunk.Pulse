# HoneyDrunk.Pulse.Sample.Worker

A minimal .NET Worker Service sample that demonstrates HoneyDrunk telemetry in a
background processing loop. It wires up `AddHoneyDrunkOpenTelemetry` (traces,
metrics, logs) and the Pulse analytics emitter, then runs a hosted `Worker` that:

- Starts an `Activity` per simulated job and tags it with the job id and status.
- Records a `sample.worker.jobs.processed` counter and a `sample.worker.job.duration` histogram.
- Emits a `Worker.JobCompleted` `TelemetryEvent` via `IAnalyticsEmitter` to the Pulse Collector.
- Logs success and failure paths via source-generated `LoggerMessage` methods.

By default it ships traces to an OTLP endpoint at `http://localhost:4317` and
analytics to a Pulse Collector at `http://localhost:5000`; override via
`HoneyDrunk:OpenTelemetry:OtlpEndpoint` and `HoneyDrunk:Pulse:CollectorEndpoint`
in configuration.

## How to run

```bash
dotnet run --project HoneyDrunk.Pulse/HoneyDrunk.Pulse.Sample.Worker/HoneyDrunk.Pulse.Sample.Worker.csproj
```

The worker processes one job per second until stopped (Ctrl+C).
