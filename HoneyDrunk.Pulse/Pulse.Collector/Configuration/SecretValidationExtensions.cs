// <copyright file="SecretValidationExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

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
    private const string AzureMonitorConnectionStringSecretName = "AzureMonitor--ConnectionString";

    /// <summary>
    /// Validates that all required secrets are available via Vault.
    /// In non-development environments, throws if any enabled sink's secrets are missing.
    /// In development, logs warnings for missing secrets when the corresponding sink is enabled.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="options">The collector options.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication ValidateRequiredSecrets(
        this WebApplication app,
        PulseCollectorOptions options)
    {
        var secretStore = app.Services.GetRequiredService<ISecretStore>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var isDevelopment = app.Environment.IsDevelopment();

        var missingSecrets = new List<string>();

        // Check PostHog API key via Vault if sink is enabled
        if (options.EnablePostHogSink)
        {
            var secretId = new SecretIdentifier(PostHogApiKeySecretName);
            var result = secretStore.TryGetSecretAsync(secretId).GetAwaiter().GetResult();
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value?.Value))
            {
                missingSecrets.Add(PostHogApiKeySecretName);
            }
        }

        // Check Sentry DSN via Vault if sink is enabled
        if (options.EnableSentrySink)
        {
            var secretId = new SecretIdentifier(SentryDsnSecretName);
            var result = secretStore.TryGetSecretAsync(secretId).GetAwaiter().GetResult();
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value?.Value))
            {
                missingSecrets.Add(SentryDsnSecretName);
            }
        }

        // Check Azure Monitor connection string via Vault if sink is enabled
        if (options.EnableAzureMonitorSink)
        {
            var secretId = new SecretIdentifier(AzureMonitorConnectionStringSecretName);
            var result = secretStore.TryGetSecretAsync(secretId).GetAwaiter().GetResult();
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value?.Value))
            {
                missingSecrets.Add(AzureMonitorConnectionStringSecretName);
            }
        }

        // Note: Tempo, Loki, and Mimir sinks validate their endpoint configuration via options validation
        // They don't require secrets - endpoints are typically internal infrastructure addresses

        // Check Azure Service Bus connection string if using Azure Service Bus adapter
        // Note: This is a secondary validation; the primary validation happens in ConfigureTransportAdapter
        // which fails fast during service registration if the secret is missing
        if (string.Equals(options.TransportAdapter, TransportValidationExtensions.AzureServiceBusAdapter, StringComparison.OrdinalIgnoreCase))
        {
            var secretId = new SecretIdentifier(TransportValidationExtensions.ServiceBusConnectionStringSecretName);
            var result = secretStore.TryGetSecretAsync(secretId).GetAwaiter().GetResult();
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value?.Value))
            {
                // This should have already failed in ConfigureTransportAdapter, but log for completeness
                missingSecrets.Add(TransportValidationExtensions.ServiceBusConnectionStringSecretName);
            }
        }

        if (missingSecrets.Count > 0)
        {
            var message = $"Required secrets are missing from Vault: {string.Join(", ", missingSecrets)}";

            if (!isDevelopment)
            {
                logger.LogFailFast(message);
                throw new InvalidOperationException($"FAIL-FAST: {message}. Configure secrets via Vault.");
            }
            else
            {
                logger.LogMissingSecretWarning(message);
            }
        }
        else
        {
            logger.LogSecretsValidated();
        }

        return app;
    }
}
