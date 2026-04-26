// <copyright file="ServiceCollectionExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Mimir.Implementation;
using HoneyDrunk.Telemetry.Sink.Mimir.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Telemetry.Sink.Mimir.Extensions;

/// <summary>
/// Extension methods for registering Mimir sink services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mimir metrics sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMimirSink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MimirSinkOptions>(
            configuration.GetSection(MimirSinkOptions.SectionName));

        return services.AddMimirSinkCore();
    }

    /// <summary>
    /// Adds the Mimir metrics sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMimirSink(
        this IServiceCollection services,
        Action<MimirSinkOptions> configureOptions)
    {
        services.Configure(configureOptions);

        return services.AddMimirSinkCore();
    }

    private static IServiceCollection AddMimirSinkCore(this IServiceCollection services)
    {
        services.AddHttpClient<MimirSink>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<MimirSinkOptions>>()
                .Value;

            if (!string.IsNullOrEmpty(options.Endpoint))
            {
                var uri = new Uri(options.Endpoint);
                client.BaseAddress = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<MimirSink>());

        return services;
    }
}
