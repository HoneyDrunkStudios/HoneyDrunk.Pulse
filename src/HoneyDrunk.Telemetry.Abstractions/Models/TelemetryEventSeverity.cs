// <copyright file="TelemetryEventSeverity.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Models;

/// <summary>
/// Defines severity levels for telemetry events.
/// </summary>
public enum TelemetryEventSeverity
{
    /// <summary>
    /// Debug level - detailed diagnostic information.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Information level - general operational information.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning level - potential issues or unexpected behavior.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error level - errors that should be investigated.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Fatal level - critical errors that may cause system failure.
    /// </summary>
    Fatal = 4,
}
