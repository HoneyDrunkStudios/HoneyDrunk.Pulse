// <copyright file="IngestionStatus.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Contracts.Events;

/// <summary>
/// Defines the status of a telemetry ingestion operation.
/// </summary>
public enum IngestionStatus
{
    /// <summary>
    /// Ingestion completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Ingestion completed with some items failing.
    /// </summary>
    PartialSuccess = 1,

    /// <summary>
    /// Ingestion failed completely.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Ingestion was skipped (e.g., due to rate limiting).
    /// </summary>
    Skipped = 3,
}
