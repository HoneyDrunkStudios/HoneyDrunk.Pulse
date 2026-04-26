// <copyright file="ServiceCollectionExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.AzureMonitor.Implementation;
using HoneyDrunk.Telemetry.Sink.AzureMonitor.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Telemetry.Sink.AzureMonitor.Extensions;

/// <summary>
/// Extension methods for registering Azure Monitor sink services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Azure Monitor sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// The Azure Monitor connection string must be provided separately via the
    /// <see cref="AddAzureMonitorSinkWithConnectionString"/> method or through
    /// Vault secret resolution in the Collector.
    /// </remarks>
    public static IServiceCollection AddAzureMonitorSink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureMonitorSinkOptions>(
            configuration.GetSection(AzureMonitorSinkOptions.SectionName));

        return services.AddAzureMonitorSinkCore(connectionString: null);
    }

    /// <summary>
    /// Adds the Azure Monitor sink to the service collection with a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="connectionString">The Azure Monitor connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureMonitorSinkWithConnectionString(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.Configure<AzureMonitorSinkOptions>(
            configuration.GetSection(AzureMonitorSinkOptions.SectionName));

        return services.AddAzureMonitorSinkCore(connectionString);
    }

    /// <summary>
    /// Adds the Azure Monitor sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <param name="connectionString">The Azure Monitor connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureMonitorSink(
        this IServiceCollection services,
        Action<AzureMonitorSinkOptions> configureOptions,
        string? connectionString = null)
    {
        services.Configure(configureOptions);

        return services.AddAzureMonitorSinkCore(connectionString);
    }

    private static IServiceCollection AddAzureMonitorSinkCore(
        this IServiceCollection services,
        string? connectionString)
    {
        services.AddHttpClient<AzureMonitorSink>();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureMonitorSinkOptions>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AzureMonitorSink));
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzureMonitorSink>>();

            return new AzureMonitorSink(httpClient, options, connectionString, logger);
        });

        // Register Azure Monitor sink under sink abstractions for multi-sink support
        // This allows the collector to fan-out to Azure Monitor alongside other sinks
        services.AddSingleton<ITraceSink>(sp => sp.GetRequiredService<AzureMonitorSink>());
        services.AddSingleton<ILogSink>(sp => sp.GetRequiredService<AzureMonitorSink>());
        services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<AzureMonitorSink>());

        return services;
    }
}
