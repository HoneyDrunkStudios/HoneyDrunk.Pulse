// <copyright file="OtlpEndpointValidationExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// Extension methods for validating OTLP endpoint configuration.
/// </summary>
public static class OtlpEndpointValidationExtensions
{
    /// <summary>
    /// The configuration key for the OTLP endpoint.
    /// </summary>
    public const string OtlpEndpointConfigKey = "HoneyDrunk:OpenTelemetry:OtlpEndpoint";

    /// <summary>
    /// Default localhost endpoint used only in Development.
    /// </summary>
    public const string LocalhostDefault = "http://localhost:4317";

    /// <summary>
    /// Well-known localhost addresses that indicate self-referencing or local-only configuration.
    /// </summary>
    private static readonly string[] LocalhostAddresses =
    [
        "localhost",
        "127.0.0.1",
        "::1",
        "[::1]",
        "0.0.0.0",
    ];

    /// <summary>
    /// Well-known OTLP gRPC and HTTP ports used by collectors.
    /// </summary>
    private static readonly int[] CollectorPorts = [4317, 4318, 5000, 5001];

    /// <summary>
    /// Gets the OTLP endpoint from configuration with environment-aware validation.
    /// In Development, allows localhost default. In non-Development, requires explicit non-localhost configuration.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The validated OTLP endpoint.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown in non-Development environments if the endpoint is missing, empty, or localhost.
    /// </exception>
    public static string GetValidatedOtlpEndpoint(this WebApplicationBuilder builder)
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var configuredEndpoint = builder.Configuration[OtlpEndpointConfigKey];

        // Development: allow localhost default
        if (isDevelopment)
        {
            return string.IsNullOrWhiteSpace(configuredEndpoint)
                ? LocalhostDefault
                : configuredEndpoint;
        }

        // Non-Development: require explicit non-localhost configuration
        if (string.IsNullOrWhiteSpace(configuredEndpoint))
        {
            throw new InvalidOperationException(
                $"FAIL-FAST: OTLP exporter endpoint is required in non-Development environments. " +
                $"Configure '{OtlpEndpointConfigKey}' with an absolute URI (e.g., 'https://otel-collector.example.com:4317').");
        }

        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var otlpUri))
        {
            throw new InvalidOperationException(
                $"FAIL-FAST: OTLP exporter endpoint '{configuredEndpoint}' is not a valid absolute URI. " +
                $"Configure '{OtlpEndpointConfigKey}' with a valid absolute URI (e.g., 'https://otel-collector.example.com:4317').");
        }

        var isLocalhost = LocalhostAddresses.Contains(otlpUri.Host, StringComparer.OrdinalIgnoreCase);
        if (isLocalhost)
        {
            throw new InvalidOperationException(
                $"FAIL-FAST: OTLP exporter endpoint '{configuredEndpoint}' resolves to localhost, which is not allowed in non-Development environments. " +
                $"Configure '{OtlpEndpointConfigKey}' with a remote collector URI (e.g., 'https://otel-collector.example.com:4317').");
        }

        return configuredEndpoint;
    }

    /// <summary>
    /// Validates that the OTLP endpoint does not point back to this collector.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="otlpEndpoint">The configured OTLP endpoint.</param>
    /// <returns>The web application for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the endpoint is self-referencing.</exception>
    public static WebApplication ValidateOtlpEndpointNotSelfReferencing(
        this WebApplication app,
        string? otlpEndpoint)
    {
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            return app;
        }

        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (!Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpUri))
        {
            // Invalid URI format - let downstream fail
            logger.LogWarning(
                "OTLP endpoint '{Endpoint}' is not a valid URI. Validation skipped.",
                otlpEndpoint);
            return app;
        }

        if (IsSelfReferencing(otlpUri, app.Urls))
        {
            var errorMessage = $"OTLP endpoint '{otlpEndpoint}' points to this collector, which would create an infinite loop. " +
                               $"Configure '{OtlpEndpointConfigKey}' to point to a different collector or remove it to disable OTLP export.";

            // Critical-level log always fires regardless of filter — pre-formatted message is cheap on the failure path.
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.LogCritical("FAIL-FAST: {Message}", errorMessage);
            }

            throw new InvalidOperationException($"FAIL-FAST: {errorMessage}");
        }

        // Warn if pointing to localhost on a typical collector port (might be intentional in Development)
        var isLocalhost = LocalhostAddresses.Contains(otlpUri.Host, StringComparer.OrdinalIgnoreCase);
        var isCollectorPort = CollectorPorts.Contains(otlpUri.Port);
        if (isLocalhost && isCollectorPort && !app.Environment.IsDevelopment())
        {
            // This should not happen after GetValidatedOtlpEndpoint, but kept as defense-in-depth
            logger.LogWarning(
                "OTLP endpoint '{Endpoint}' points to localhost on port {Port}. " +
                "This should not occur in non-Development environments.",
                otlpEndpoint,
                otlpUri.Port);
        }

        return app;
    }

    private static bool IsSelfReferencing(Uri otlpUri, IEnumerable<string> listeningUrls)
    {
        var urls = listeningUrls.ToList();
        if (urls.Count == 0)
        {
            urls.Add("http://localhost:5000");
            urls.Add("https://localhost:5001");
        }

        return urls.Any(url => Uri.TryCreate(url, UriKind.Absolute, out var listenUri)
            && PointsToSameEndpoint(otlpUri, listenUri));
    }

    private static bool PointsToSameEndpoint(Uri otlpUri, Uri listenUri)
    {
        if (otlpUri.Port != listenUri.Port)
        {
            return false;
        }

        if (string.Equals(otlpUri.Host, listenUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Both ends being any localhost variant on the same port is also self-referencing.
        return LocalhostAddresses.Contains(otlpUri.Host, StringComparer.OrdinalIgnoreCase)
            && LocalhostAddresses.Contains(listenUri.Host, StringComparer.OrdinalIgnoreCase);
    }
}
