// <copyright file="Program.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Hosting;
using HoneyDrunk.Pulse.Collector.Configuration;
using HoneyDrunk.Pulse.Collector.Endpoints;
using HoneyDrunk.Pulse.Collector.Enrichment;
using HoneyDrunk.Pulse.Collector.Ingestion;
using HoneyDrunk.Pulse.Collector.Services;
using HoneyDrunk.Pulse.Collector.Transport;
using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.OpenTelemetry.Extensions;
using HoneyDrunk.Telemetry.Sink.AzureMonitor.Extensions;
using HoneyDrunk.Telemetry.Sink.Loki.Extensions;
using HoneyDrunk.Telemetry.Sink.Loki.Options;
using HoneyDrunk.Telemetry.Sink.Mimir.Extensions;
using HoneyDrunk.Telemetry.Sink.PostHog.Extensions;
using HoneyDrunk.Telemetry.Sink.Sentry.Extensions;
using HoneyDrunk.Telemetry.Sink.Tempo.Extensions;
using HoneyDrunk.Vault.EventGrid.Extensions;
using HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Pulse.Collector;

/// <summary>
/// Entry point for the Pulse Collector service.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous host lifetime.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration["HONEYDRUNK_NODE_ID"] ??= "pulse";
        builder.Services.Replace(ServiceDescriptor.Singleton<IConfiguration>(builder.Configuration));

        builder.Services.AddHoneyDrunkNode(options =>
        {
            options.NodeId = new NodeId("pulse");
            options.SectorId = SectorId.WellKnown.Ops;
            options.StudioId = "honeydrunk";
            options.EnvironmentId = new EnvironmentId(builder.Environment.EnvironmentName.ToLowerInvariant());
        })
        .AddVaultWithAzureKeyVaultBootstrap()
        .AddAppConfiguration();

        // Bind options once and register the same instance into DI (single source of truth)
        var collectorOptions = new PulseCollectorOptions();
        builder.Configuration.GetSection(PulseCollectorOptions.SectionName).Bind(collectorOptions);
        builder.Services.AddSingleton(Options.Create(collectorOptions));

        // Validate and get OTLP endpoint (fail-fast in non-Development if missing/localhost)
        var otlpEndpoint = builder.GetValidatedOtlpEndpoint();

        // Add OpenTelemetry instrumentation
        builder.Services.AddHoneyDrunkOpenTelemetry(options =>
        {
            options.ServiceName = collectorOptions.ServiceName;
            options.Environment = collectorOptions.Environment;
            options.OtlpEndpoint = otlpEndpoint;
        });

        // Add sinks (conditionally)
        if (collectorOptions.EnablePostHogSink)
        {
            builder.Services.AddPostHogSink(builder.Configuration);
        }

        if (collectorOptions.EnableSentrySink)
        {
            builder.Services.AddSentrySink(builder.Configuration);
        }

        if (collectorOptions.EnableTempoSink)
        {
            builder.Services.AddTempoSink(builder.Configuration);
        }

        if (collectorOptions.EnableLokiSink)
        {
            builder.Services.AddLokiSink(builder.Configuration);
        }

        if (collectorOptions.EnableMimirSink)
        {
            builder.Services.AddMimirSink(builder.Configuration);
        }

        if (collectorOptions.EnableAzureMonitorSink)
        {
            builder.Services.AddAzureMonitorSink(builder.Configuration);
        }

        // Configure Transport adapter (validates environment compatibility)
        builder.ConfigureTransportAdapter(collectorOptions);

        // Register core services
        builder.Services.AddSingleton<OtlpParser>();
        builder.Services.AddSingleton<PulseIngestedPublisher>();
        builder.Services.AddScoped<TelemetryEnricher>();

        // Register IngestionPipeline with factory to handle multiple sinks per signal type
        builder.Services.AddScoped<IngestionPipeline>(sp =>
        {
            var enricher = sp.GetRequiredService<TelemetryEnricher>();
            var analyticsSink = sp.GetService<IAnalyticsSink>();
            var errorSink = sp.GetService<IErrorSink>();
            var traceSinks = sp.GetServices<ITraceSink>();
            var logSinks = sp.GetServices<ILogSink>();
            var metricsSinks = sp.GetServices<IMetricsSink>();
            var publisher = sp.GetRequiredService<PulseIngestedPublisher>();
            var options = sp.GetRequiredService<IOptions<PulseCollectorOptions>>();
            var lokiOptions = sp.GetService<IOptions<LokiSinkOptions>>();
            var logger = sp.GetRequiredService<ILogger<IngestionPipeline>>();

            return new IngestionPipeline(
                enricher,
                analyticsSink,
                errorSink,
                traceSinks,
                logSinks,
                metricsSinks,
                publisher,
                options,
                lokiOptions,
                logger);
        });

        // Add gRPC services for OTLP receiver
        builder.Services.AddGrpc();

        // Add health checks
        builder.Services.AddHealthChecks();
        builder.Services.AddVaultEventGridInvalidation();

        var app = builder.Build();

        // Validate OTLP endpoint is not self-referencing (fail-fast to prevent infinite loop)
        app.ValidateOtlpEndpointNotSelfReferencing(otlpEndpoint);

        // Validate required secrets (fail-fast in production if missing).
        // Reads are parallelized inside the extension; await it once at startup before serving traffic.
        await app.ValidateRequiredSecretsAsync(collectorOptions).ConfigureAwait(false);

        // Validate sink endpoints (fail-fast in production if missing)
        app.ValidateSinkEndpoints(collectorOptions);

        // Map endpoints
        app.MapHealthEndpoints();
        app.MapOtlpEndpoints();
        app.MapVaultInvalidationWebhook("/internal/vault/invalidate");

        // Map gRPC OTLP services
        app.MapGrpcService<OtlpTraceService>();
        app.MapGrpcService<OtlpMetricsService>();
        app.MapGrpcService<OtlpLogsService>();

        await app.RunAsync().ConfigureAwait(false);
    }
}
