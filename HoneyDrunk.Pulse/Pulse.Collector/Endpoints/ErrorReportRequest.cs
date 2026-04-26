// <copyright file="ErrorReportRequest.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Models;

namespace HoneyDrunk.Pulse.Collector.Endpoints;

/// <summary>
/// Request model for error reports.
/// </summary>
public sealed class ErrorReportRequest
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public TelemetryEventSeverity? Severity { get; set; }

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the operation ID.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the node ID.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the environment.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets tags.
    /// </summary>
    public Dictionary<string, string>? Tags { get; init; }

    /// <summary>
    /// Gets extra contextual data.
    /// </summary>
    public Dictionary<string, object?>? Extra { get; init; }
}
