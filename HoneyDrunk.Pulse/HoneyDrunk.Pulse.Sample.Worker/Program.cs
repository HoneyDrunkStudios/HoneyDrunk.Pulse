// <copyright file="Program.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Pulse.Sample.Worker;
using HoneyDrunk.Telemetry.OpenTelemetry.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Add HoneyDrunk OpenTelemetry instrumentation
builder.Services.AddHoneyDrunkOpenTelemetry(options =>
{
    options.ServiceName = "Sample.Worker";
    options.Environment = builder.Environment.EnvironmentName;
    options.OtlpEndpoint = builder.Configuration["HoneyDrunk:OpenTelemetry:OtlpEndpoint"]
        ?? "http://localhost:4317";
});

// Add Pulse Analytics Emitter for sending analytics events to Pulse.Collector
builder.Services.AddPulseAnalyticsEmitter(options =>
{
    options.CollectorEndpoint = builder.Configuration["HoneyDrunk:Pulse:CollectorEndpoint"]
        ?? "http://localhost:5000";
    options.ServiceName = "Sample.Worker";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
