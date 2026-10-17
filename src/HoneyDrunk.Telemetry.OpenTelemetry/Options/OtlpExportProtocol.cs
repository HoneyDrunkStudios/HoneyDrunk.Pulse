// <copyright file="OtlpExportProtocol.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.OpenTelemetry.Options;

/// <summary>
/// Defines the OTLP export protocol options.
/// </summary>
public enum OtlpExportProtocol
{
    /// <summary>
    /// Use gRPC protocol for OTLP export.
    /// </summary>
    Grpc = 0,

    /// <summary>
    /// Use HTTP/Protobuf protocol for OTLP export.
    /// </summary>
    HttpProtobuf = 1,
}
