// <copyright file="SinkFailureScenarioTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Pulse.Collector.Configuration;
using HoneyDrunk.Pulse.Collector.Enrichment;
using HoneyDrunk.Pulse.Collector.Ingestion;
using HoneyDrunk.Pulse.Collector.Transport;
using HoneyDrunk.Pulse.Contracts.Events;
using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Transport.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Tests for sink failure scenarios in the IngestionPipeline.
/// Validates that partial failures are handled correctly and reported accurately.
/// </summary>
public class SinkFailureScenarioTests
{
    private readonly FakeMessagePublisher _messagePublisher;
    private readonly PulseIngestedPublisher _publisher;
    private readonly TelemetryEnricher _enricher;
    private readonly IOptions<PulseCollectorOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SinkFailureScenarioTests"/> class.
    /// </summary>
    public SinkFailureScenarioTests()
    {
        _messagePublisher = new FakeMessagePublisher();

        var gridContextAccessor = new FakeGridContextAccessor();
        var nodeContext = new FakeNodeContext();

        _publisher = new PulseIngestedPublisher(
            _messagePublisher,
            gridContextAccessor,
            nodeContext,
            NullLogger<PulseIngestedPublisher>.Instance);

        _options = Options.Create(new PulseCollectorOptions
        {
            EnablePostHogSink = true,
            EnableSentrySink = true,
            EnableTransportPublishing = true,
            EnableTempoSink = true,
            EnableLokiSink = true,
            EnableMimirSink = true,
        });

        _enricher = new TelemetryEnricher(
            operationContextAccessor: null,
            _options,
            NullLogger<TelemetryEnricher>.Instance);
    }

    /// <summary>
    /// Scenario A: Tempo throws during trace export, other sinks succeed.
    /// Expected: IngestionStatus.PartialSuccess, other sinks receive data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WhenTempoFails_ReturnsPartialSuccess()
    {
        // Arrange
        var failingTempoSink = new FailingTraceSink("Tempo connection refused");
        var successfulAzureMonitorSink = new SuccessfulTraceSink();
        var rawOtlpData = new byte[] { 0x01, 0x02, 0x03 };

        var pipeline = CreatePipeline(
            traceSinks: [failingTempoSink, successfulAzureMonitorSink],
            logSinks: [],
            metricsSinks: []);

        // Act
        await pipeline.ProcessTracesAsync(
            traceCount: 5,
            sourceName: "test-service",
            sourceNodeId: "node-1",
            errorSpans: null,
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf");

        // Assert - Ingestion did not crash
        _messagePublisher.PublishedMessages.Should().HaveCount(1);

        var message = _messagePublisher.PublishedMessages[0].Should().BeOfType<PulseIngested>().Subject;

        // Assert - Status is PartialSuccess (not Success, not Failure)
        message.Status.Should().Be(IngestionStatus.PartialSuccess);

        // Assert - Successful sink was called
        successfulAzureMonitorSink.ExportCallCount.Should().Be(1);
        successfulAzureMonitorSink.LastReceivedData.Should().NotBeNull();

        // Assert - Failing sink was attempted
        failingTempoSink.ExportCallCount.Should().Be(1);

        // Assert - Error context in metadata
        message.ErrorMessage.Should().Contain("1 sink(s) failed");
        message.Metadata.Should().ContainKey("pulse.sink_failures");
        message.Metadata["pulse.sink_failures"].Should().Be("1");
    }

    /// <summary>
    /// Scenario A continued: Verify error spans still forwarded to Sentry when Tempo fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WhenTempoFails_ErrorSpansStillForwardedToSentry()
    {
        // Arrange
        var failingTempoSink = new FailingTraceSink("Tempo unavailable");
        var errorSink = new SuccessfulErrorSink();
        var rawOtlpData = new byte[] { 0x01, 0x02, 0x03 };

        var errorSpans = new List<ExtractedErrorSpan>
        {
            new(
                SpanName: "GET /api/users",
                ServiceName: "user-service",
                ErrorMessage: "Database connection failed",
                ExceptionType: "System.Data.SqlClient.SqlException",
                ExceptionMessage: "Timeout expired",
                StackTrace: "at UserRepository.GetAll()",
                TraceId: "trace-abc",
                SpanId: "span-123",
                Attributes: new Dictionary<string, string> { ["http.status_code"] = "500" }),
        };

        var pipeline = CreatePipeline(
            traceSinks: [failingTempoSink],
            logSinks: [],
            metricsSinks: [],
            errorSink: errorSink);

        // Act
        await pipeline.ProcessTracesAsync(
            traceCount: 1,
            sourceName: "user-service",
            sourceNodeId: "node-1",
            errorSpans: errorSpans,
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf");

        // Assert - Error spans were still forwarded to Sentry
        errorSink.CapturedErrors.Should().HaveCount(1);
        errorSink.CapturedErrors[0].Message.Should().Contain("Database connection failed");
    }

    /// <summary>
    /// Scenario B: Loki throws during log export, Azure Monitor succeeds.
    /// Expected: IngestionStatus.PartialSuccess, Azure Monitor receives data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessLogsAsync_WhenLokiFails_ReturnsPartialSuccess()
    {
        // Arrange
        var failingLokiSink = new FailingLogSink("Loki rate limit exceeded");
        var successfulAzureMonitorSink = new SuccessfulLogSink();
        var rawOtlpData = new byte[] { 0x0A, 0x0B, 0x0C };

        var pipeline = CreatePipeline(
            traceSinks: [],
            logSinks: [failingLokiSink, successfulAzureMonitorSink],
            metricsSinks: []);

        // Act
        await pipeline.ProcessLogsAsync(
            logCount: 100,
            sourceName: "log-service",
            sourceNodeId: "node-2",
            errorLogs: null,
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf",
            maxSeverityNumber: 17); // Error level

        // Assert - Ingestion did not crash
        _messagePublisher.PublishedMessages.Should().HaveCount(1);

        var message = _messagePublisher.PublishedMessages[0].Should().BeOfType<PulseIngested>().Subject;

        // Assert - Status is PartialSuccess
        message.Status.Should().Be(IngestionStatus.PartialSuccess);

        // Assert - Successful sink was called
        successfulAzureMonitorSink.ExportCallCount.Should().Be(1);

        // Assert - Failing sink was attempted
        failingLokiSink.ExportCallCount.Should().Be(1);

        // Assert - Failure metadata
        message.Metadata["pulse.sink_failures"].Should().Be("1");
    }

    /// <summary>
    /// Scenario B continued: Verify error logs still forwarded to Sentry when Loki fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessLogsAsync_WhenLokiFails_ErrorLogsStillForwardedToSentry()
    {
        // Arrange
        var failingLokiSink = new FailingLogSink("Loki unavailable");
        var errorSink = new SuccessfulErrorSink();
        var rawOtlpData = new byte[] { 0x0A, 0x0B };

        var errorLogs = new List<ExtractedErrorLog>
        {
            new(
                Message: "Unhandled exception in payment processor",
                ServiceName: "payment-service",
                SeverityNumber: 17,
                SeverityText: "ERROR",
                TraceId: "trace-pay-1",
                SpanId: "span-pay-1",
                Timestamp: DateTimeOffset.UtcNow,
                ExceptionType: "PaymentException",
                ExceptionMessage: "Card declined",
                StackTrace: "at PaymentProcessor.Process()",
                Attributes: new Dictionary<string, string> { ["payment.id"] = "pay-123" }),
        };

        var pipeline = CreatePipeline(
            traceSinks: [],
            logSinks: [failingLokiSink],
            metricsSinks: [],
            errorSink: errorSink);

        // Act
        await pipeline.ProcessLogsAsync(
            logCount: 1,
            sourceName: "payment-service",
            sourceNodeId: "node-pay",
            errorLogs: errorLogs,
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf",
            maxSeverityNumber: 17);

        // Assert - Error logs were still forwarded to Sentry
        errorSink.CapturedErrors.Should().HaveCount(1);
        errorSink.CapturedErrors[0].Message.Should().Contain("Unhandled exception");
    }

    /// <summary>
    /// Scenario C: Azure Monitor throws during trace export, Tempo succeeds.
    /// Expected: IngestionStatus.PartialSuccess, Tempo receives data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WhenAzureMonitorFails_TempoSucceeds_ReturnsPartialSuccess()
    {
        // Arrange
        var successfulTempoSink = new SuccessfulTraceSink();
        var failingAzureMonitorSink = new FailingTraceSink("Azure Monitor 401 Unauthorized");
        var rawOtlpData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var pipeline = CreatePipeline(
            traceSinks: [successfulTempoSink, failingAzureMonitorSink],
            logSinks: [],
            metricsSinks: []);

        // Act
        await pipeline.ProcessTracesAsync(
            traceCount: 10,
            sourceName: "api-service",
            sourceNodeId: "node-api",
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf");

        // Assert
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.Status.Should().Be(IngestionStatus.PartialSuccess);

        // Tempo received data
        successfulTempoSink.ExportCallCount.Should().Be(1);
        successfulTempoSink.LastReceivedData!.Value.ToArray().Should().BeEquivalentTo(rawOtlpData);

        // Azure Monitor was attempted
        failingAzureMonitorSink.ExportCallCount.Should().Be(1);
    }

    /// <summary>
    /// Scenario C: Azure Monitor fails for metrics, Mimir succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessMetricsAsync_WhenAzureMonitorFails_MimirSucceeds_ReturnsPartialSuccess()
    {
        // Arrange
        var successfulMimirSink = new SuccessfulMetricsSink();
        var failingAzureMonitorSink = new FailingMetricsSink("Azure Monitor 503 Service Unavailable");
        var rawOtlpData = new byte[] { 0xAA, 0xBB, 0xCC };

        var pipeline = CreatePipeline(
            traceSinks: [],
            logSinks: [],
            metricsSinks: [successfulMimirSink, failingAzureMonitorSink]);

        // Act
        await pipeline.ProcessMetricsAsync(
            metricCount: 50,
            sourceName: "metrics-source",
            sourceNodeId: "node-metrics",
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf");

        // Assert
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.Status.Should().Be(IngestionStatus.PartialSuccess);

        successfulMimirSink.ExportCallCount.Should().Be(1);
        failingAzureMonitorSink.ExportCallCount.Should().Be(1);
        message.Metadata["pulse.sink_failures"].Should().Be("1");
    }

    /// <summary>
    /// Scenario C: Azure Monitor fails for logs, Loki succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessLogsAsync_WhenAzureMonitorFails_LokiSucceeds_ReturnsPartialSuccess()
    {
        // Arrange
        var successfulLokiSink = new SuccessfulLogSink();
        var failingAzureMonitorSink = new FailingLogSink("Azure Monitor timeout");
        var rawOtlpData = new byte[] { 0xDD, 0xEE, 0xFF };

        var pipeline = CreatePipeline(
            traceSinks: [],
            logSinks: [successfulLokiSink, failingAzureMonitorSink],
            metricsSinks: []);

        // Act
        await pipeline.ProcessLogsAsync(
            logCount: 25,
            sourceName: "app-service",
            sourceNodeId: "node-app",
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf",
            maxSeverityNumber: 13); // Warning level

        // Assert
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.Status.Should().Be(IngestionStatus.PartialSuccess);

        successfulLokiSink.ExportCallCount.Should().Be(1);
        failingAzureMonitorSink.ExportCallCount.Should().Be(1);
    }

    /// <summary>
    /// Scenario D: All trace sinks fail.
    /// Expected: IngestionStatus.PartialSuccess (ingestion itself succeeded, sinks failed).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WhenAllSinksFail_ReturnsPartialSuccess()
    {
        // Arrange
        var failingTempoSink = new FailingTraceSink("Tempo down");
        var failingAzureMonitorSink = new FailingTraceSink("Azure Monitor down");
        var rawOtlpData = new byte[] { 0x11, 0x22 };

        var pipeline = CreatePipeline(
            traceSinks: [failingTempoSink, failingAzureMonitorSink],
            logSinks: [],
            metricsSinks: []);

        // Act
        await pipeline.ProcessTracesAsync(
            traceCount: 3,
            sourceName: "failing-test",
            sourceNodeId: "node-fail",
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf");

        // Assert - Still publishes event (ingestion succeeded, routing failed)
        _messagePublisher.PublishedMessages.Should().HaveCount(1);

        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.Status.Should().Be(IngestionStatus.PartialSuccess);

        // Both sinks were attempted
        failingTempoSink.ExportCallCount.Should().Be(1);
        failingAzureMonitorSink.ExportCallCount.Should().Be(1);

        // Error message indicates 2 failures
        message.ErrorMessage.Should().Contain("2 sink(s) failed");
        message.Metadata["pulse.sink_failures"].Should().Be("2");
    }

    /// <summary>
    /// Scenario D: Multiple sink types fail across different signals.
    /// Tests that each signal type tracks failures independently.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessAllSignals_WithMultipleFailures_EachReportsCorrectly()
    {
        // Arrange
        var failingTraceSink1 = new FailingTraceSink("Tempo network error");
        var failingTraceSink2 = new FailingTraceSink("Azure Monitor auth error");
        var failingLogSink = new FailingLogSink("Loki disk full");
        var successfulMetricsSink = new SuccessfulMetricsSink();

        var pipeline = CreatePipeline(
            traceSinks: [failingTraceSink1, failingTraceSink2],
            logSinks: [failingLogSink],
            metricsSinks: [successfulMetricsSink]);

        var rawOtlpData = new byte[] { 0x99 };

        // Act - Process traces (2 failures)
        await pipeline.ProcessTracesAsync(1, "svc", "n1", rawOtlpData: rawOtlpData, contentType: "application/x-protobuf");

        // Act - Process logs (1 failure)
        await pipeline.ProcessLogsAsync(1, "svc", "n1", rawOtlpData: rawOtlpData, contentType: "application/x-protobuf", maxSeverityNumber: 17);

        // Act - Process metrics (0 failures)
        await pipeline.ProcessMetricsAsync(1, "svc", "n1", rawOtlpData: rawOtlpData, contentType: "application/x-protobuf");

        // Assert - 3 messages published
        _messagePublisher.PublishedMessages.Should().HaveCount(3);

        // Trace message: 2 failures
        var traceMessage = _messagePublisher.PublishedMessages[0] as PulseIngested;
        traceMessage!.Status.Should().Be(IngestionStatus.PartialSuccess);
        traceMessage.Metadata["pulse.sink_failures"].Should().Be("2");

        // Log message: 1 failure
        var logMessage = _messagePublisher.PublishedMessages[1] as PulseIngested;
        logMessage!.Status.Should().Be(IngestionStatus.PartialSuccess);
        logMessage.Metadata["pulse.sink_failures"].Should().Be("1");

        // Metrics message: 0 failures (Success)
        var metricsMessage = _messagePublisher.PublishedMessages[2] as PulseIngested;
        metricsMessage!.Status.Should().Be(IngestionStatus.Success);
        metricsMessage.Metadata.Should().NotContainKey("pulse.sink_failures");
    }

    /// <summary>
    /// When no sinks are registered, ingestion still succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WithNoSinks_ReturnsSuccess()
    {
        // Arrange
        var pipeline = CreatePipeline(
            traceSinks: [],
            logSinks: [],
            metricsSinks: []);

        // Act
        await pipeline.ProcessTracesAsync(5, "no-sink-service", "node-1");

        // Assert
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.Status.Should().Be(IngestionStatus.Success);
        message.Metadata.Should().NotContainKey("pulse.sink_failures");
    }

    /// <summary>
    /// When all sinks succeed, status is Success.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WhenAllSinksSucceed_ReturnsSuccess()
    {
        // Arrange
        var successfulTempoSink = new SuccessfulTraceSink();
        var successfulAzureMonitorSink = new SuccessfulTraceSink();
        var rawOtlpData = new byte[] { 0xAB, 0xCD };

        var pipeline = CreatePipeline(
            traceSinks: [successfulTempoSink, successfulAzureMonitorSink],
            logSinks: [],
            metricsSinks: []);

        // Act
        await pipeline.ProcessTracesAsync(
            traceCount: 10,
            sourceName: "healthy-service",
            sourceNodeId: "node-healthy",
            rawOtlpData: rawOtlpData,
            contentType: "application/x-protobuf");

        // Assert
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.Status.Should().Be(IngestionStatus.Success);

        // Both sinks were called
        successfulTempoSink.ExportCallCount.Should().Be(1);
        successfulAzureMonitorSink.ExportCallCount.Should().Be(1);

        // No failure metadata
        message.Metadata.Should().NotContainKey("pulse.sink_failures");
    }

    /// <summary>
    /// Analytics sink failure results in PartialSuccess.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessAnalyticsEventsAsync_WhenPostHogFails_ReturnsPartialSuccess()
    {
        // Arrange
        var failingAnalyticsSink = new FailingAnalyticsSink("PostHog quota exceeded");

        var pipeline = CreatePipeline(
            traceSinks: [],
            logSinks: [],
            metricsSinks: [],
            analyticsSink: failingAnalyticsSink);

        var events = new List<TelemetryEvent>
        {
            TelemetryEvent.Create("user_signed_up").WithDistinctId("user-123"),
        };

        // Act
        await pipeline.ProcessAnalyticsEventsAsync(events, "auth-service", "node-auth");

        // Assert
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.Status.Should().Be(IngestionStatus.PartialSuccess);
        failingAnalyticsSink.CaptureCallCount.Should().Be(1);
    }

    private IngestionPipeline CreatePipeline(
        IEnumerable<ITraceSink> traceSinks,
        IEnumerable<ILogSink> logSinks,
        IEnumerable<IMetricsSink> metricsSinks,
        IAnalyticsSink? analyticsSink = null,
        IErrorSink? errorSink = null)
    {
        return new IngestionPipeline(
            _enricher,
            analyticsSink,
            errorSink,
            traceSinks,
            logSinks,
            metricsSinks,
            _publisher,
            _options,
            lokiOptions: null,
            NullLogger<IngestionPipeline>.Instance);
    }

    /// <summary>
    /// Trace sink that always throws.
    /// </summary>
    private sealed class FailingTraceSink(string errorMessage) : ITraceSink
    {
        public int ExportCallCount { get; private set; }

        public Task ExportAsync(ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default)
        {
            ExportCallCount++;
            throw new InvalidOperationException(errorMessage);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Trace sink that always succeeds.
    /// </summary>
    private sealed class SuccessfulTraceSink : ITraceSink
    {
        public int ExportCallCount { get; private set; }

        public ReadOnlyMemory<byte>? LastReceivedData { get; private set; }

        public string? LastContentType { get; private set; }

        public Task ExportAsync(ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default)
        {
            ExportCallCount++;
            LastReceivedData = data;
            LastContentType = contentType;
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Log sink that always throws.
    /// </summary>
    private sealed class FailingLogSink(string errorMessage) : ILogSink
    {
        public int ExportCallCount { get; private set; }

        public Task ExportAsync(ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default)
        {
            ExportCallCount++;
            throw new InvalidOperationException(errorMessage);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Log sink that always succeeds.
    /// </summary>
    private sealed class SuccessfulLogSink : ILogSink
    {
        public int ExportCallCount { get; private set; }

        public ReadOnlyMemory<byte>? LastReceivedData { get; private set; }

        public Task ExportAsync(ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default)
        {
            ExportCallCount++;
            LastReceivedData = data;
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Metrics sink that always throws.
    /// </summary>
    private sealed class FailingMetricsSink(string errorMessage) : IMetricsSink
    {
        public int ExportCallCount { get; private set; }

        public Task ExportAsync(ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default)
        {
            ExportCallCount++;
            throw new InvalidOperationException(errorMessage);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Metrics sink that always succeeds.
    /// </summary>
    private sealed class SuccessfulMetricsSink : IMetricsSink
    {
        public int ExportCallCount { get; private set; }

        public ReadOnlyMemory<byte>? LastReceivedData { get; private set; }

        public Task ExportAsync(ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default)
        {
            ExportCallCount++;
            LastReceivedData = data;
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Error sink that always succeeds and captures events.
    /// </summary>
    private sealed class SuccessfulErrorSink : IErrorSink
    {
        public List<ErrorEvent> CapturedErrors { get; } = [];

        public Task CaptureAsync(ErrorEvent errorEvent, CancellationToken cancellationToken = default)
        {
            CapturedErrors.Add(errorEvent);
            return Task.CompletedTask;
        }

        public Task CaptureExceptionAsync(Exception exception, IDictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
        {
            CapturedErrors.Add(ErrorEvent.FromException(exception));
            return Task.CompletedTask;
        }

        public Task CaptureMessageAsync(string message, TelemetryEventSeverity severity = TelemetryEventSeverity.Error, CancellationToken cancellationToken = default)
        {
            CapturedErrors.Add(ErrorEvent.FromMessage(message));
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Analytics sink that always throws.
    /// </summary>
    private sealed class FailingAnalyticsSink(string errorMessage) : IAnalyticsSink
    {
        public int CaptureCallCount { get; private set; }

        public Task CaptureAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
        {
            CaptureCallCount++;
            throw new InvalidOperationException(errorMessage);
        }

        public Task CaptureBatchAsync(IEnumerable<TelemetryEvent> events, CancellationToken cancellationToken = default)
        {
            CaptureCallCount++;
            throw new InvalidOperationException(errorMessage);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMessagePublisher : IMessagePublisher
    {
        public List<object> PublishedMessages { get; } = [];

        public Task PublishAsync<T>(string destination, T message, IGridContext gridContext, CancellationToken cancellationToken = default)
            where T : class
        {
            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishBatchAsync<T>(string destination, IEnumerable<T> messages, IGridContext gridContext, CancellationToken cancellationToken = default)
            where T : class
        {
            foreach (var message in messages)
            {
                PublishedMessages.Add(message);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeGridContextAccessor : IGridContextAccessor
    {
        public IGridContext GridContext { get; set; } = null!;
    }

    private sealed class FakeNodeContext : INodeContext
    {
        public string NodeId => "test-node-sink-failures";

        public string Version => "1.0.0";

        public string StudioId => "test-studio";

        public string Environment => "Test";

        public NodeLifecycleStage LifecycleStage { get; private set; } = NodeLifecycleStage.Ready;

        public DateTimeOffset StartedAtUtc => DateTimeOffset.UtcNow;

        public string MachineName => "test-machine";

        public int ProcessId => 99999;

        public IReadOnlyDictionary<string, string> Tags => new Dictionary<string, string>();

        public void SetLifecycleStage(NodeLifecycleStage stage) => LifecycleStage = stage;
    }
}
