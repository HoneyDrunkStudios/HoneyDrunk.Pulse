// <copyright file="SecretValidationExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Sink.AzureMonitor.Options;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// Extension methods for validating required secrets at startup.
/// </summary>
public static class SecretValidationExtensions
{
    private const string PostHogApiKeySecretName = "PostHog--ApiKey";
    private const string SentryDsnSecretName = "Sentry--Dsn";

    // Source of truth for the AzureMonitor secret name lives on the sink options package so that
    // a single rename propagates everywhere.
    private const string AzureMonitorConnectionStringSecretName = AzureMonitorSinkOptions.ConnectionStringSecretKey;

    /// <summary>
    /// Validates that all required secrets are available via Vault.
    /// In non-development environments, throws if any enabled sink's secrets are missing.
    /// In development, logs warnings for missing secrets when the corresponding sink is enabled.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="options">The collector options.</param>
    /// <returns>The web application for chaining.</returns>
    /// <remarks>
    /// All required secret reads are dispatched in parallel via <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{Task})"/>
    /// so that startup latency is bounded by the slowest read rather than the sum of all reads.
    /// </remarks>
    public static async Task<WebApplication> ValidateRequiredSecretsAsync(
        this WebApplication app,
        PulseCollectorOptions options)
    {
        var secretStore = app.Services.GetRequiredService<ISecretStore>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var isDevelopment = app.Environment.IsDevelopment();

        // Collect all checks to run; each returns the secret name only on miss, otherwise null.
        var checks = new List<Task<string?>>();

        if (options.EnablePostHogSink)
        {
            checks.Add(CheckSecretAsync(secretStore, PostHogApiKeySecretName));
        }

        if (options.EnableSentrySink)
        {
            checks.Add(CheckSecretAsync(secretStore, SentryDsnSecretName));
        }

        if (options.EnableAzureMonitorSink)
        {
            checks.Add(CheckSecretAsync(secretStore, AzureMonitorConnectionStringSecretName));
        }

        // Note: Tempo, Loki, and Mimir sinks validate their endpoint configuration via options validation.
        // They don't require secrets — endpoints are typically internal infrastructure addresses.

        // Check Azure Service Bus connection string if using the Service Bus transport adapter.
        // This is a secondary validation; the primary one happens in ConfigureTransportAdapter, which
        // fails fast during service registration if the secret is missing.
        if (string.Equals(options.TransportAdapter, TransportValidationExtensions.AzureServiceBusAdapter, StringComparison.OrdinalIgnoreCase))
        {
            checks.Add(CheckSecretAsync(secretStore, TransportValidationExtensions.ServiceBusConnectionStringSecretName));
        }

        var results = await Task.WhenAll(checks).ConfigureAwait(false);
        var missingSecrets = results.Where(name => name is not null).Cast<string>().ToList();

        if (missingSecrets.Count > 0)
        {
            var message = $"Required secrets are missing from Vault: {string.Join(", ", missingSecrets)}";

            if (!isDevelopment)
            {
                logger.LogFailFast(message);
                throw new InvalidOperationException($"FAIL-FAST: {message}. Configure secrets via Vault.");
            }

            logger.LogMissingSecretWarning(message);
        }
        else if (checks.Count > 0)
        {
            // Only emit the success log when at least one Vault read actually happened.
            // If no sinks that require secrets are enabled, there's nothing to validate.
            logger.LogSecretsValidated();
        }

        return app;
    }

    /// <summary>
    /// Reads a secret and returns its name if missing or empty, otherwise null.
    /// </summary>
    private static async Task<string?> CheckSecretAsync(ISecretStore secretStore, string secretName)
    {
        var result = await secretStore
            .TryGetSecretAsync(new SecretIdentifier(secretName))
            .ConfigureAwait(false);

        return !result.IsSuccess || string.IsNullOrWhiteSpace(result.Value?.Value)
            ? secretName
            : null;
    }
}
