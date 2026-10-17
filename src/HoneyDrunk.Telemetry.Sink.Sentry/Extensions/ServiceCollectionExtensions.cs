// <copyright file="ServiceCollectionExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Sentry.Implementation;
using HoneyDrunk.Telemetry.Sink.Sentry.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Telemetry.Sink.Sentry.Extensions;

/// <summary>
/// Extension methods for registering Sentry sink services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Sentry error tracking sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSentrySink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SentrySinkOptions>(
            configuration.GetSection(SentrySinkOptions.SectionName));

        return services.AddSentrySinkCore();
    }

    /// <summary>
    /// Adds the Sentry error tracking sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSentrySink(
        this IServiceCollection services,
        Action<SentrySinkOptions> configureOptions)
    {
        services.Configure(configureOptions);

        return services.AddSentrySinkCore();
    }

    private static IServiceCollection AddSentrySinkCore(this IServiceCollection services)
    {
        services.AddSingleton<SentrySink>();
        services.AddSingleton<IErrorSink>(sp => sp.GetRequiredService<SentrySink>());

        return services;
    }
}
