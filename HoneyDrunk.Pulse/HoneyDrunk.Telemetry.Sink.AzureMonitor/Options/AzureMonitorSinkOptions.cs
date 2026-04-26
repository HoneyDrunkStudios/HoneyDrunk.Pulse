// <copyright file="AzureMonitorSinkOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.AzureMonitor.Options;

/// <summary>
/// Configuration options for the Azure Monitor sink.
/// </summary>
public sealed class AzureMonitorSinkOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:AzureMonitor";

    /// <summary>
    /// The Vault secret name for the Azure Monitor connection string.
    /// </summary>
    /// <remarks>
    /// Uses the <c>{Provider}--{Key}</c> convention shared across the Grid (matches
    /// <c>Resend--ApiKey</c>, <c>Twilio--AccountSid</c>, etc.). Consumers reading this secret via
    /// <c>ISecretStore</c> must use this exact identifier.
    /// </remarks>
    public const string ConnectionStringSecretKey = "AzureMonitor--ConnectionString";

    /// <summary>
    /// Gets or sets a value indicating whether the sink is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to export traces to Azure Monitor.
    /// </summary>
    public bool ExportTraces { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to export metrics to Azure Monitor.
    /// </summary>
    public bool ExportMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to export logs to Azure Monitor.
    /// </summary>
    public bool ExportLogs { get; set; } = true;

    /// <summary>
    /// Gets or sets the cloud role name for Application Insights.
    /// </summary>
    /// <remarks>
    /// This corresponds to the service name in Application Insights.
    /// If not set, the service.name resource attribute is used.
    /// </remarks>
    public string? CloudRoleName { get; set; }

    /// <summary>
    /// Gets or sets the cloud role instance.
    /// </summary>
    /// <remarks>
    /// If not set, the service.instance.id resource attribute or machine name is used.
    /// </remarks>
    public string? CloudRoleInstance { get; set; }

    /// <summary>
    /// Gets or sets the sampling ratio (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// Default is 1.0 (100% sampling). Set to a lower value to reduce telemetry volume.
    /// </remarks>
    public double SamplingRatio { get; set; } = 1.0;
}
