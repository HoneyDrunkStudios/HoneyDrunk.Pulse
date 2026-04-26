// <copyright file="ServiceCollectionExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Loki.Implementation;
using HoneyDrunk.Telemetry.Sink.Loki.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Telemetry.Sink.Loki.Extensions;

/// <summary>
/// Extension methods for registering Loki sink services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Loki log sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLokiSink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LokiSinkOptions>(
            configuration.GetSection(LokiSinkOptions.SectionName));

        return services.AddLokiSinkCore();
    }

    /// <summary>
    /// Adds the Loki log sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLokiSink(
        this IServiceCollection services,
        Action<LokiSinkOptions> configureOptions)
    {
        services.Configure(configureOptions);

        return services.AddLokiSinkCore();
    }

    private static IServiceCollection AddLokiSinkCore(this IServiceCollection services)
    {
        services.AddHttpClient<LokiSink>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<LokiSinkOptions>>()
                .Value;

            if (!string.IsNullOrEmpty(options.Endpoint))
            {
                client.BaseAddress = new Uri(options.Endpoint, UriKind.Absolute);
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddSingleton<ILogSink>(sp => sp.GetRequiredService<LokiSink>());

        return services;
    }
}
