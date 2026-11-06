// Copyright (c) HoneyDrunk Studios. All rights reserved.

namespace HoneyDrunk.Telemetry.Sink.Tempo.Options;

/// <summary>
/// The protocol to use for Tempo communication.
/// </summary>
public enum TempoProtocol
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
