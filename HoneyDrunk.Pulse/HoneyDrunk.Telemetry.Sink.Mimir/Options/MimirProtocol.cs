// <copyright file="MimirProtocol.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Mimir.Options;

/// <summary>
/// The protocol to use for Mimir communication.
/// </summary>
public enum MimirProtocol
{
    /// <summary>
    /// OTLP over HTTP/JSON or HTTP/protobuf.
    /// </summary>
    Http,

    /// <summary>
    /// OTLP over gRPC.
    /// </summary>
    Grpc,
}
