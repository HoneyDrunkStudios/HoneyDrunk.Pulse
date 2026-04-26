// <copyright file="LokiProtocol.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Loki.Options;

/// <summary>
/// The protocol to use for Loki communication.
/// </summary>
public enum LokiProtocol
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
