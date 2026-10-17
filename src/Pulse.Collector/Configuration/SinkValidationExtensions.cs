// <copyright file="SinkValidationExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Sink.Loki.Options;
using HoneyDrunk.Telemetry.Sink.Mimir.Options;
using HoneyDrunk.Telemetry.Sink.Tempo.Options;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// Extension methods for validating sink configurations at startup.
/// </summary>
public static class SinkValidationExtensions
{
    /// <summary>
    /// Validates sink endpoint configurations for enabled sinks.
    /// In non-development environments, throws if any enabled sink's endpoint is missing.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="options">The collector options.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication ValidateSinkEndpoints(
        this WebApplication app,
        PulseCollectorOptions options)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var isDevelopment = app.Environment.IsDevelopment();

        var missingEndpoints = new List<string>();

        // Validate Tempo sink endpoint
        if (options.EnableTempoSink)
        {
            var tempoOptions = app.Services.GetRequiredService<IOptions<TempoSinkOptions>>().Value;
            if (string.IsNullOrWhiteSpace(tempoOptions.Endpoint))
            {
                missingEndpoints.Add("Tempo (HoneyDrunk:Tempo:Endpoint)");
            }
        }

        // Validate Loki sink endpoint
        if (options.EnableLokiSink)
        {
            var lokiOptions = app.Services.GetRequiredService<IOptions<LokiSinkOptions>>().Value;
            if (string.IsNullOrWhiteSpace(lokiOptions.Endpoint))
            {
                missingEndpoints.Add("Loki (HoneyDrunk:Loki:Endpoint)");
            }
        }

        // Validate Mimir sink endpoint
        if (options.EnableMimirSink)
        {
            var mimirOptions = app.Services.GetRequiredService<IOptions<MimirSinkOptions>>().Value;
            if (string.IsNullOrWhiteSpace(mimirOptions.Endpoint))
            {
                missingEndpoints.Add("Mimir (HoneyDrunk:Mimir:Endpoint)");
            }
        }

        if (missingEndpoints.Count > 0)
        {
            var message = $"Required sink endpoints are missing: {string.Join(", ", missingEndpoints)}";

            if (!isDevelopment)
            {
                logger.LogSinkValidationFailFast(message);
                throw new InvalidOperationException($"FAIL-FAST: {message}. Configure sink endpoints in appsettings.");
            }
            else
            {
                logger.LogMissingSinkEndpointsWarning(message);
            }
        }
        else if (options.EnableTempoSink || options.EnableLokiSink || options.EnableMimirSink)
        {
            logger.LogSinkEndpointsValidated();
        }

        return app;
    }
}
