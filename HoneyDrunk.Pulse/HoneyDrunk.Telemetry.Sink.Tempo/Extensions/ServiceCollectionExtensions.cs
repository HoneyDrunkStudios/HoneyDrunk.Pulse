// <copyright file="ServiceCollectionExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Tempo.Implementation;
using HoneyDrunk.Telemetry.Sink.Tempo.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Telemetry.Sink.Tempo.Extensions;

/// <summary>
/// Extension methods for registering Tempo sink services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Tempo trace sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTempoSink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TempoSinkOptions>(
            configuration.GetSection(TempoSinkOptions.SectionName));

        return services.AddTempoSinkCore();
    }

    /// <summary>
    /// Adds the Tempo trace sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTempoSink(
        this IServiceCollection services,
        Action<TempoSinkOptions> configureOptions)
    {
        services.Configure(configureOptions);

        return services.AddTempoSinkCore();
    }

    private static IServiceCollection AddTempoSinkCore(this IServiceCollection services)
    {
        services.AddHttpClient<TempoSink>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<TempoSinkOptions>>()
                .Value;

            if (!string.IsNullOrEmpty(options.Endpoint))
            {
                client.BaseAddress = new Uri(options.Endpoint, UriKind.Absolute);
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddSingleton<ITraceSink>(sp => sp.GetRequiredService<TempoSink>());

        return services;
    }
}
