// <copyright file="FeatureUsedRequest.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Sample.Api;

/// <summary>
/// Request model for feature usage tracking.
/// </summary>
/// <param name="FeatureName">The name of the feature used.</param>
/// <param name="UserId">The user ID.</param>
/// <param name="DurationMs">Duration of feature usage in milliseconds.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated via JSON deserialization")]
internal sealed record FeatureUsedRequest(string FeatureName, string? UserId, long? DurationMs);
