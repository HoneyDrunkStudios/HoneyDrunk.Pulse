// <copyright file="ServiceCollectionExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Conventions;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using HoneyDrunk.Telemetry.OpenTelemetry.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HoneyDrunk.Telemetry.OpenTelemetry.Extensions;

/// <summary>
/// Extension methods for configuring HoneyDrunk OpenTelemetry services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds HoneyDrunk OpenTelemetry instrumentation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new HoneyDrunkOpenTelemetryOptions();
        configuration.GetSection(HoneyDrunkOpenTelemetryOptions.SectionName).Bind(options);

        return services.AddHoneyDrunkOpenTelemetry(options);
    }

    /// <summary>
    /// Adds HoneyDrunk OpenTelemetry instrumentation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkOpenTelemetry(
        this IServiceCollection services,
        Action<HoneyDrunkOpenTelemetryOptions> configureOptions)
    {
        var options = new HoneyDrunkOpenTelemetryOptions();
        configureOptions(options);

        return services.AddHoneyDrunkOpenTelemetry(options);
    }

    /// <summary>
    /// Adds HoneyDrunk OpenTelemetry instrumentation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The OpenTelemetry options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkOpenTelemetry(
        this IServiceCollection services,
        HoneyDrunkOpenTelemetryOptions options)
    {
        // Build the resource with HoneyDrunk conventions
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceNamespace: options.ServiceNamespace,
                serviceVersion: options.ServiceVersion,
                serviceInstanceId: options.ServiceInstanceId ?? Environment.MachineName);

        // Add environment attributes
        if (!string.IsNullOrEmpty(options.Environment))
        {
            resourceBuilder.AddAttributes(
            [
                new KeyValuePair<string, object>(TelemetryTagKeys.Semantic.DeploymentEnvironment, options.Environment),
                new KeyValuePair<string, object>(TelemetryTagKeys.HoneyDrunk.Environment, options.Environment),
            ]);
        }

        // Configure OpenTelemetry
        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddDetector(new ResourceDetector(resourceBuilder)));

        // Configure Tracing
        if (options.EnableTracing)
        {
            otelBuilder.WithTracing(tracing =>
            {
                ConfigureTracing(tracing, options);
            });
        }

        // Configure Metrics
        if (options.EnableMetrics)
        {
            otelBuilder.WithMetrics(metrics =>
            {
                ConfigureMetrics(metrics, options);
            });
        }

        // Configure Logging
        if (options.EnableLogging)
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(otelLogging =>
                {
                    otelLogging.SetResourceBuilder(resourceBuilder);
                    otelLogging.IncludeFormattedMessage = true;
                    otelLogging.IncludeScopes = true;

                    ConfigureOtlpExporter(
                        otelLogging,
                        options.OtlpEndpoint,
                        options.ExportProtocol);
                });
            });
        }

        return services;
    }

    /// <summary>
    /// Adds the Pulse analytics emitter to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPulseAnalyticsEmitter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new PulseAnalyticsEmitterOptions();
        configuration.GetSection(PulseAnalyticsEmitterOptions.SectionName).Bind(options);

        return services.AddPulseAnalyticsEmitter(options);
    }

    /// <summary>
    /// Adds the Pulse analytics emitter to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPulseAnalyticsEmitter(
        this IServiceCollection services,
        Action<PulseAnalyticsEmitterOptions> configureOptions)
    {
        var options = new PulseAnalyticsEmitterOptions();
        configureOptions(options);

        return services.AddPulseAnalyticsEmitter(options);
    }

    /// <summary>
    /// Adds the Pulse analytics emitter to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The analytics emitter options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPulseAnalyticsEmitter(
        this IServiceCollection services,
        PulseAnalyticsEmitterOptions options)
    {
        services.Configure<PulseAnalyticsEmitterOptions>(o =>
        {
            o.CollectorEndpoint = options.CollectorEndpoint;
            o.ServiceName = options.ServiceName;
            o.TimeoutSeconds = options.TimeoutSeconds;
        });

        services.AddHttpClient(PulseAnalyticsEmitterOptions.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(options.CollectorEndpoint);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            if (!string.IsNullOrEmpty(options.ServiceName))
            {
                client.DefaultRequestHeaders.Add("X-Source-Service", options.ServiceName);
            }
        });

        services.AddSingleton<IAnalyticsEmitter, PulseAnalyticsEmitter>();

        return services;
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, HoneyDrunkOpenTelemetryOptions options)
    {
        // Add HoneyDrunk activity sources
        tracing.AddSource(TelemetryNames.ActivitySources.PulseCollector);
        tracing.AddSource(TelemetryNames.ActivitySources.Kernel);
        tracing.AddSource(TelemetryNames.ActivitySources.Transport);
        tracing.AddSource(TelemetryNames.ActivitySources.Vault);
        tracing.AddSource(TelemetryNames.GetActivitySourceName(options.ServiceName));

        // Add additional configured activity sources
        foreach (var source in options.AdditionalActivitySources)
        {
            tracing.AddSource(source);
        }

        // ASP.NET Core instrumentation
        if (options.EnableAspNetCoreInstrumentation)
        {
            tracing.AddAspNetCoreInstrumentation(aspNetCoreOptions =>
            {
                aspNetCoreOptions.RecordException = true;
            });
        }

        // HTTP client instrumentation
        if (options.EnableHttpClientInstrumentation)
        {
            tracing.AddHttpClientInstrumentation(httpOptions =>
            {
                httpOptions.RecordException = true;
            });
        }

        // Configure OTLP exporter
        ConfigureOtlpExporter(tracing, options.OtlpEndpoint, options.ExportProtocol);
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, HoneyDrunkOpenTelemetryOptions options)
    {
        // Add HoneyDrunk meters
        metrics.AddMeter(TelemetryNames.Meters.PulseCollector);
        metrics.AddMeter(TelemetryNames.Meters.Kernel);
        metrics.AddMeter(TelemetryNames.GetMeterName(options.ServiceName));

        // Add additional configured meters
        foreach (var meter in options.AdditionalMeters)
        {
            metrics.AddMeter(meter);
        }

        // ASP.NET Core instrumentation
        if (options.EnableAspNetCoreInstrumentation)
        {
            metrics.AddAspNetCoreInstrumentation();
        }

        // HTTP client instrumentation
        if (options.EnableHttpClientInstrumentation)
        {
            metrics.AddHttpClientInstrumentation();
        }

        // Runtime instrumentation
        if (options.EnableRuntimeInstrumentation)
        {
            metrics.AddRuntimeInstrumentation();
        }

        // Process instrumentation
        if (options.EnableProcessInstrumentation)
        {
            metrics.AddProcessInstrumentation();
        }

        // Configure OTLP exporter
        ConfigureOtlpExporter(metrics, options.OtlpEndpoint, options.ExportProtocol);
    }

    private static void ConfigureOtlpExporter(
        TracerProviderBuilder builder,
        string endpoint,
        Options.OtlpExportProtocol protocol)
    {
        builder.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(endpoint);
            otlp.Protocol = protocol == Options.OtlpExportProtocol.Grpc
                ? global::OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                : global::OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        });
    }

    private static void ConfigureOtlpExporter(
        MeterProviderBuilder builder,
        string endpoint,
        Options.OtlpExportProtocol protocol)
    {
        builder.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(endpoint);
            otlp.Protocol = protocol == Options.OtlpExportProtocol.Grpc
                ? global::OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                : global::OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        });
    }

    private static void ConfigureOtlpExporter(
        OpenTelemetryLoggerOptions builder,
        string endpoint,
        Options.OtlpExportProtocol protocol)
    {
        builder.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(endpoint);
            otlp.Protocol = protocol == Options.OtlpExportProtocol.Grpc
                ? global::OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                : global::OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        });
    }

    /// <summary>
    /// A simple resource detector that adds the pre-configured resource.
    /// </summary>
    private sealed class ResourceDetector(ResourceBuilder resourceBuilder) : IResourceDetector
    {
        public Resource Detect()
        {
            return resourceBuilder.Build();
        }
    }
}
