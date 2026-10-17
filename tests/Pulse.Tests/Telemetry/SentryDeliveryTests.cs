using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using HoneyDrunk.Telemetry.Sink.Sentry.Implementation;
using HoneyDrunk.Telemetry.Sink.Sentry.Options;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Sentry.Extensibility;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>Verifies SDK mapping and secret rotation with an in-memory transport.</summary>
[Collection("Sentry lifecycle")]
public sealed class SentryDeliveryTests
{
    /// <summary>Vault versions control initialization and events retain correlation and severity.</summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task CapturePreservesContextAndReinitializesAfterSecretRotation()
    {
        var store = Substitute.For<ISecretStore>();
        var version = "one";
        store.GetSecretAsync(Arg.Any<SecretIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(call => new SecretValue(call.Arg<SecretIdentifier>(), "https://public@example.test/1", version));
        var captured = new List<SentryEvent>();
        var initializations = 0;
        var options = new SentrySinkOptions { Environment = "test", Release = "1.2.3" };
        options.DefaultTags["default"] = "present";
        using var sink = new SentrySink(store, Options.Create(options), NullLogger<SentrySink>.Instance, configure =>
            SentrySdk.Init(sentryOptions =>
            {
                configure(sentryOptions);
                initializations++;
                sentryOptions.Transport = Substitute.For<ITransport>();
                sentryOptions.SetBeforeSend(evt =>
                {
                    captured.Add(evt);
                    return null;
                });
            }));
        var error = ErrorEvent.FromException(new InvalidOperationException("example"));
        error.CorrelationId = "correlation";
        error.OperationId = "operation";
        error.NodeId = "node";
        error.UserId = "user";
        error.Tags["custom"] = "value";
        error.Extra["attempt"] = 2;
        await sink.CaptureAsync(error);
        var first = Assert.Single(captured);
        Assert.Equal("correlation", first.Tags[TelemetryTagKeys.HoneyDrunk.CorrelationId]);
        Assert.Equal("operation", first.Tags[TelemetryTagKeys.HoneyDrunk.OperationId]);
        Assert.Equal("node", first.Tags[TelemetryTagKeys.HoneyDrunk.NodeId]);
        Assert.Equal("user", first.User.Id);
        Assert.Equal("value", first.Tags["custom"]);
        Assert.Equal("present", first.Tags["default"]);
        Assert.Equal(2, first.Extra["attempt"]);
        Assert.Equal("test", first.Environment);
        Assert.Equal("1.2.3", first.Release);

        foreach (var severity in Enum.GetValues<TelemetryEventSeverity>())
        {
            await sink.CaptureMessageAsync("message-" + severity, severity);
        }

        Assert.Equal(1, initializations);
        Assert.Contains(captured, evt => evt.Level == SentryLevel.Fatal);
        Assert.Contains(captured, evt => evt.Level == SentryLevel.Debug);
        await sink.CaptureExceptionAsync(new ArgumentException("argument"), new Dictionary<string, string> { ["exception"] = "tag" });
        Assert.Equal("tag", captured[^1].Tags["exception"]);
        version = "two";
        var sanitized = ErrorEvent.FromMessage("sanitized");
        sanitized.Tags["exception.type"] = "Example.Error";
        await sink.CaptureAsync(sanitized);
        Assert.Equal(2, initializations);
        Assert.Equal("Example.Error", Assert.Single(captured[^1].SentryExceptions!).Type);
        await sink.CaptureAsync(ErrorEvent.FromMessage("message-only"));
        var count = captured.Count;
        await sink.CaptureAsync(new ErrorEvent());
        Assert.Equal(count, captured.Count);
        await sink.FlushAsync();
        sink.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sink.CaptureAsync(error));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sink.CaptureMessageAsync("disposed"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sink.CaptureExceptionAsync(new InvalidOperationException()));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sink.FlushAsync());
    }

    /// <summary>A disabled sink neither reads secrets nor initializes the SDK.</summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task DisabledSinkDoesNotReadVault()
    {
        var store = Substitute.For<ISecretStore>();
        using var sink = new SentrySink(store, Options.Create(new SentrySinkOptions { Enabled = false }), NullLogger<SentrySink>.Instance);
        await sink.CaptureAsync(new ErrorEvent());
        await sink.CaptureExceptionAsync(new InvalidOperationException());
        await sink.CaptureMessageAsync("disabled");
        await sink.FlushAsync();
        Assert.Empty(store.ReceivedCalls());
    }
}
