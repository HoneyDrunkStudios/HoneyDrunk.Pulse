// <copyright file="Program.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Pulse.Sample.Api;
using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Conventions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.OpenTelemetry.Extensions;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Register ActivitySource as a DI singleton so the container owns its lifecycle and disposes it
// at host shutdown. This avoids the local-not-disposed analyzer flagging an explicit
// new ActivitySource(...) at top-level statements (CodeQL doesn't model the
// ApplicationStopping.Register(activitySource.Dispose) pattern as proper disposal).
builder.Services.AddSingleton(_ =>
    new ActivitySource(TelemetryNames.GetActivitySourceName("Sample.Api")));

// Add HoneyDrunk OpenTelemetry instrumentation
builder.Services.AddHoneyDrunkOpenTelemetry(options =>
{
    options.ServiceName = "Sample.Api";
    options.Environment = builder.Environment.EnvironmentName;
    options.OtlpEndpoint = builder.Configuration["HoneyDrunk:OpenTelemetry:OtlpEndpoint"]
        ?? "http://localhost:4317";
});

// Add Pulse Analytics Emitter for sending analytics events to Pulse.Collector
builder.Services.AddPulseAnalyticsEmitter(options =>
{
    options.CollectorEndpoint = builder.Configuration["HoneyDrunk:Pulse:CollectorEndpoint"]
        ?? "http://localhost:5000";
    options.ServiceName = "Sample.Api";
});

var app = builder.Build();

// Resolve the DI-owned ActivitySource for use in minimal-API endpoint handlers below.
// The container calls Dispose on it during host shutdown.
var activitySource = app.Services.GetRequiredService<ActivitySource>();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
};

// Successful endpoint with tracing
app.MapGet("/weatherforecast", () =>
{
    using var activity = activitySource.StartActivity("GetWeatherForecast");
    activity?.SetTag("forecast.count", 5);

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]))
        .ToArray();

    activity?.SetTag("forecast.generated", true);
    return forecast;
});

// Endpoint that throws an exception (for error tracking demo)
app.MapGet("/error", () =>
{
    using var activity = activitySource.StartActivity("SimulateError");
    activity?.SetTag("error.simulated", true);

    throw new InvalidOperationException("This is a simulated error for testing error tracking.");
});

// Endpoint that emits an analytics event
app.MapPost("/analytics/feature-used", async (FeatureUsedRequest request, IAnalyticsEmitter analyticsEmitter) =>
{
    using var activity = activitySource.StartActivity("RecordFeatureUsage");

    // Create a TelemetryEvent for analytics
    var telemetryEvent = TelemetryEvent.Create(TelemetryNames.GetEventName("Feature", "Used"))
        .WithDistinctId(request.UserId ?? "anonymous")
        .WithProperty("feature_name", request.FeatureName)
        .WithProperty("duration_ms", request.DurationMs);

    activity?.SetTag("analytics.event_name", telemetryEvent.EventName);
    activity?.SetTag("analytics.feature", request.FeatureName);

    // Send the analytics event to Pulse.Collector via the abstraction
    await analyticsEmitter.EmitAsync(telemetryEvent).ConfigureAwait(false);
    activity?.SetTag("analytics.sent", true);

    return Results.Ok(new { Status = "sent", telemetryEvent.EventName });
});

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
