// <copyright file="Worker.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Conventions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HoneyDrunk.Pulse.Sample.Worker;

/// <summary>
/// Sample background worker demonstrating HoneyDrunk telemetry instrumentation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Worker"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="analyticsEmitter">The analytics emitter for sending events to Pulse.Collector.</param>
public sealed partial class Worker(ILogger<Worker> logger, IAnalyticsEmitter analyticsEmitter) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryNames.GetActivitySourceName("Sample.Worker"));
    private static readonly Meter WorkerMeter = new(TelemetryNames.GetMeterName("Sample.Worker"));
    private readonly Counter<long> _jobsProcessedCounter = WorkerMeter.CreateCounter<long>(
            "sample.worker.jobs.processed",
            "jobs",
            "Number of jobs processed by the worker");

    private readonly Histogram<double> _jobDurationHistogram = WorkerMeter.CreateHistogram<double>(
            "sample.worker.job.duration",
            "ms",
            "Duration of job processing in milliseconds");

    private int _jobCounter;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _jobCounter++;

            using var activity = ActivitySource.StartActivity("ProcessJob", ActivityKind.Internal);
            activity?.SetTag("job.id", _jobCounter);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                LogJobRunning(_jobCounter, DateTimeOffset.Now);

                // Simulate job processing
                await Task.Delay(Random.Shared.Next(100, 500), stoppingToken).ConfigureAwait(false);

                // Record success
                activity?.SetTag("job.status", "completed");
                _jobsProcessedCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));

                // Create an analytics event for the job
                var telemetryEvent = TelemetryEvent.Create(TelemetryNames.GetEventName("Worker", "JobCompleted"))
                    .WithProperty("job_id", _jobCounter)
                    .WithProperty("duration_ms", stopwatch.ElapsedMilliseconds);

                // Send the analytics event to Pulse.Collector
                await SendAnalyticsEventAsync(telemetryEvent, stoppingToken).ConfigureAwait(false);

                LogJobCompleted(_jobCounter, telemetryEvent.EventName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                LogJobError(ex, _jobCounter);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("job.status", "failed");
                _jobsProcessedCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));
            }
            finally
            {
                stopwatch.Stop();
                _jobDurationHistogram.Record(stopwatch.ElapsedMilliseconds);
            }

            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task SendAnalyticsEventAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken)
    {
        await analyticsEmitter.EmitAsync(telemetryEvent, cancellationToken).ConfigureAwait(false);
    }
}
