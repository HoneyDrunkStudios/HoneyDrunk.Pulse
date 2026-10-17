// <copyright file="PulseAnalyticsRedactionTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.OpenTelemetry;
using HoneyDrunk.Telemetry.OpenTelemetry.Redaction;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Verifies the analytics emitter sanitizes data before sending it over HTTP.
/// </summary>
public sealed class PulseAnalyticsRedactionTests
{
    /// <summary>
    /// The HTTP payload contains sanitized copies and preserves ordinary correlation values.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task EmitAsync_RedactsBeforeHttpTransmission()
    {
        using var handler = new CapturingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://collector.example.test") };
        var emitter = new PulseAnalyticsEmitter(
            new ClientFactory(client),
            Options.Create(new PulseAnalyticsEmitterOptions()),
            NullLogger<PulseAnalyticsEmitter>.Instance);
        var telemetryEvent = TelemetryEvent.Create("order.completed").WithCorrelationId("business-42");
        telemetryEvent.UserId = "somebody@example.test";
        telemetryEvent.Properties["api_key"] = "transmission-sensitive";
        telemetryEvent.Properties["payload"] = new Dictionary<string, object?> { ["PhoneNumber"] = "555-1234" };

        await emitter.EmitAsync(telemetryEvent);

        handler.Body.Should().NotBeNull();
        handler.Body.Should().NotContain("transmission-sensitive").And.NotContain("somebody@example.test").And.NotContain("555-1234");
        handler.Body.Should().Contain("business-42").And.Contain(TelemetryRedactor.RedactedValue);
        telemetryEvent.Properties["api_key"].Should().Be("transmission-sensitive");
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }
}
