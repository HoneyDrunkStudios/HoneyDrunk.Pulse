// <copyright file="HealthEndpoints.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Endpoints;

/// <summary>
/// Health and readiness endpoint configuration.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health and readiness endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }))
            .WithName("Health")
            .WithTags("Health");

        endpoints.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready" }))
            .WithName("Ready")
            .WithTags("Health");

        endpoints.MapGet("/health/live", () => Results.Ok(new { Status = "Live" }))
            .WithName("Liveness")
            .WithTags("Health");

        return endpoints;
    }
}
