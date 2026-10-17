using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.AzureMonitor.Implementation;
using HoneyDrunk.Telemetry.Sink.AzureMonitor.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>Tests receipt logging, signal switches and lifecycle boundaries.</summary>
public sealed class AzureMonitorReceiptTests
{
    /// <summary>Each enabled signal reports either receipt or missing configuration.</summary>
    /// <param name="configured">Whether a connection string is available.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnabledSignalsReportTheirReceipt(bool configured)
    {
        var logger = Substitute.For<ILogger<AzureMonitorSink>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        using var sink = new AzureMonitorSink(Options.Create(new AzureMonitorSinkOptions()), configured ? "configured" : null, logger);
        await ((ITraceSink)sink).ExportAsync(new byte[] { 1 }, "application/json", CancellationToken.None);
        await ((ILogSink)sink).ExportAsync(new byte[] { 2 }, "application/json", CancellationToken.None);
        await ((IMetricsSink)sink).ExportAsync(new byte[] { 3 }, "application/json", CancellationToken.None);
        await ((ITraceSink)sink).FlushAsync(CancellationToken.None);
        await ((ILogSink)sink).FlushAsync(CancellationToken.None);
        await ((IMetricsSink)sink).FlushAsync(CancellationToken.None);
        var calls = logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Log").ToArray();
        Assert.Equal(3, calls.Length);
        Assert.All(calls, call => Assert.Equal(configured ? LogLevel.Debug : LogLevel.Warning, call.GetArguments()[0]));
    }

    /// <summary>Disabled signals do no work, while enabled signals honor disposal and cancellation.</summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task SignalSwitchesAndLifetimeAreEnforced()
    {
        var options = new AzureMonitorSinkOptions { Enabled = false };
        var logger = Substitute.For<ILogger<AzureMonitorSink>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        using var sink = new AzureMonitorSink(Options.Create(options), "configured", logger);
        await ((ITraceSink)sink).ExportAsync(default, "application/json", CancellationToken.None);
        await ((ILogSink)sink).ExportAsync(default, "application/json", CancellationToken.None);
        await ((IMetricsSink)sink).ExportAsync(default, "application/json", CancellationToken.None);
        options.Enabled = true;
        options.ExportTraces = false;
        options.ExportLogs = false;
        options.ExportMetrics = false;
        await ((ITraceSink)sink).ExportAsync(default, "application/json", CancellationToken.None);
        await ((ILogSink)sink).ExportAsync(default, "application/json", CancellationToken.None);
        await ((IMetricsSink)sink).ExportAsync(default, "application/json", CancellationToken.None);
        Assert.Empty(logger.ReceivedCalls());
        options.ExportTraces = true;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => ((ITraceSink)sink).ExportAsync(default, "application/json", cancelled.Token));
        sink.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => ((ITraceSink)sink).ExportAsync(default, "application/json", CancellationToken.None));
    }
}
