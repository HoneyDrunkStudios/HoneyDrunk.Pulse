// <copyright file="TransportValidationExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Transport.AzureServiceBus.DependencyInjection;
using HoneyDrunk.Transport.InMemory.DependencyInjection;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// Extension methods for validating and configuring Transport adapters.
/// </summary>
public static class TransportValidationExtensions
{
    /// <summary>
    /// Well-known transport adapter that is only allowed in Development.
    /// </summary>
    public const string InMemoryAdapter = "InMemory";

    /// <summary>
    /// Well-known production transport adapter for Azure Service Bus.
    /// </summary>
    public const string AzureServiceBusAdapter = "AzureServiceBus";

    /// <summary>
    /// Secret identifier for Azure Service Bus connection string.
    /// </summary>
    public const string ServiceBusConnectionStringSecretName = "AzureServiceBus--ConnectionString";

    /// <summary>
    /// Configures the Transport adapter based on options with environment validation.
    /// In non-Development environments, InMemory Transport is not allowed.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="options">The collector options.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown in non-Development environments if InMemory Transport is selected or no production-safe transport is configured.
    /// </exception>
    public static void ConfigureTransportAdapter(
        this WebApplicationBuilder builder,
        PulseCollectorOptions options)
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var transportAdapter = options.TransportAdapter?.Trim();

        // Validate transport adapter is specified
        if (string.IsNullOrWhiteSpace(transportAdapter))
        {
            if (isDevelopment)
            {
                // Default to InMemory in Development
                transportAdapter = InMemoryAdapter;
            }
            else
            {
                throw new InvalidOperationException(
                    $"FAIL-FAST: Transport adapter is not configured. " +
                    $"Environment: '{builder.Environment.EnvironmentName}'. " +
                    $"Configure '{PulseCollectorOptions.SectionName}:TransportAdapter' with '{AzureServiceBusAdapter}'. " +
                    $"InMemory Transport is not permitted outside Development.");
            }
        }

        // Handle InMemory adapter
        if (string.Equals(transportAdapter, InMemoryAdapter, StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    $"FAIL-FAST: InMemory Transport is not permitted in non-Development environments. " +
                    $"Environment: '{builder.Environment.EnvironmentName}'. " +
                    $"Requested adapter: '{transportAdapter}'. " +
                    $"Allowed adapters: '{AzureServiceBusAdapter}'. " +
                    $"Configure '{PulseCollectorOptions.SectionName}:TransportAdapter' with a production-safe adapter to prevent silent data loss.");
            }

            // Development: register InMemory Transport
            builder.Services.AddHoneyDrunkInMemoryTransport();
            return;
        }

        // Handle Azure Service Bus adapter
        if (string.Equals(transportAdapter, AzureServiceBusAdapter, StringComparison.OrdinalIgnoreCase))
        {
            ConfigureAzureServiceBusTransport(builder, options, isDevelopment);
            return;
        }

        // Unknown adapter
        throw new InvalidOperationException(
            $"FAIL-FAST: Unknown Transport adapter '{transportAdapter}'. " +
            $"Environment: '{builder.Environment.EnvironmentName}'. " +
            $"Allowed adapters: '{InMemoryAdapter}' (Development only), '{AzureServiceBusAdapter}'.");
    }

    private static void ConfigureAzureServiceBusTransport(
        WebApplicationBuilder builder,
        PulseCollectorOptions options,
        bool isDevelopment)
    {
        // Validate queue/topic name is configured
        if (string.IsNullOrWhiteSpace(options.ServiceBusQueueOrTopicName))
        {
            throw new InvalidOperationException(
                $"FAIL-FAST: Azure Service Bus queue or topic name is not configured. " +
                $"Environment: '{builder.Environment.EnvironmentName}'. " +
                $"Configure '{PulseCollectorOptions.SectionName}:ServiceBusQueueOrTopicName' with the target queue or topic name.");
        }

        // Build a temporary service provider to resolve ISecretStore for connection string
        // This is necessary because we need the connection string during service registration
        var tempProvider = builder.Services.BuildServiceProvider();
        var secretStore = tempProvider.GetRequiredService<ISecretStore>();

        // Resolve connection string from Vault
        var secretId = new SecretIdentifier(ServiceBusConnectionStringSecretName);
        var secretResult = secretStore.TryGetSecretAsync(secretId).GetAwaiter().GetResult();

        if (!secretResult.IsSuccess || string.IsNullOrWhiteSpace(secretResult.Value?.Value))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    $"FAIL-FAST: Azure Service Bus connection string secret is missing from Vault. " +
                    $"Environment: '{builder.Environment.EnvironmentName}'. " +
                    $"Configure secret '{ServiceBusConnectionStringSecretName}' in Vault with a valid Azure Service Bus connection string.");
            }
            else
            {
                // In Development with Azure Service Bus selected but no connection string, we could fall back to InMemory
                // But the user explicitly chose AzureServiceBus, so fail with helpful message
                throw new InvalidOperationException(
                    $"FAIL-FAST: Azure Service Bus connection string secret is missing from Vault. " +
                    $"Environment: '{builder.Environment.EnvironmentName}'. " +
                    $"Configure secret '{ServiceBusConnectionStringSecretName}' in Vault, " +
                    $"or set TransportAdapter to '{InMemoryAdapter}' for Development.");
            }
        }

        var connectionString = secretResult.Value.Value;

        // Register Azure Service Bus Transport
        builder.Services.AddHoneyDrunkServiceBusTransport(
            connectionString,
            options.ServiceBusQueueOrTopicName);
    }
}
