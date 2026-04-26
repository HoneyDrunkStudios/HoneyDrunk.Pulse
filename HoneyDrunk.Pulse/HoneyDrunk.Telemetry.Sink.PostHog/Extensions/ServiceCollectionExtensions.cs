// <copyright file="ServiceCollectionExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.PostHog.Implementation;
using HoneyDrunk.Telemetry.Sink.PostHog.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Telemetry.Sink.PostHog.Extensions;

/// <summary>
/// Extension methods for registering PostHog sink services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the PostHog analytics sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostHogSink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PostHogSinkOptions>(
            configuration.GetSection(PostHogSinkOptions.SectionName));

        return services.AddPostHogSinkCore();
    }

    /// <summary>
    /// Adds the PostHog analytics sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostHogSink(
        this IServiceCollection services,
        Action<PostHogSinkOptions> configureOptions)
    {
        services.Configure(configureOptions);

        return services.AddPostHogSinkCore();
    }

    private static IServiceCollection AddPostHogSinkCore(this IServiceCollection services)
    {
        services.AddHttpClient<PostHogSink>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PostHogSinkOptions>>().Value;
            client.BaseAddress = new Uri(options.Host);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<IAnalyticsSink>(sp => sp.GetRequiredService<PostHogSink>());

        return services;
    }
}
