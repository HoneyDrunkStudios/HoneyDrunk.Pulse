// <copyright file="IngestionPipelineTests.cs" company="HoneyDrunk Studios">
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
/// Tests for the IngestionPipeline class.
/// </summary>
public class IngestionPipelineTests
{
    private readonly FakeAnalyticsSink _analyticsSink;
    private readonly FakeErrorSink _errorSink;
    private readonly FakeMessagePublisher _messagePublisher;
    private readonly PulseIngestedPublisher _publisher;
    private readonly TelemetryEnricher _enricher;
    private readonly IOptions<PulseCollectorOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipelineTests"/> class.
    /// </summary>
    public IngestionPipelineTests()
    {
        _analyticsSink = new FakeAnalyticsSink();
        _errorSink = new FakeErrorSink();
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
        });

        _enricher = new TelemetryEnricher(
            operationContextAccessor: null,
            _options,
            NullLogger<TelemetryEnricher>.Instance);
    }

    /// <summary>
    /// Tests that ProcessTracesAsync publishes an ingestion event.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_PublishesIngestionEvent()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act
        await pipeline.ProcessTracesAsync(5, "test-service", "node-1");

        // Assert
        _messagePublisher.PublishedMessages.Should().HaveCount(1);
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message.Should().NotBeNull();
        message!.TraceCount.Should().Be(5);
        message.SourceNodeName.Should().Be("test-service");
        message.SourceNodeId.Should().Be("node-1");
        message.Status.Should().Be(IngestionStatus.Success);
    }

    /// <summary>
    /// Tests that ProcessTracesAsync forwards error spans to Sentry.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WithErrorSpans_ForwardsToSentry()
    {
        // Arrange
        var pipeline = CreatePipeline();

        var errorSpans = new List<ExtractedErrorSpan>
        {
            new(
                SpanName: "GET /api",
                ServiceName: "test-service",
                ErrorMessage: "Internal error",
                ExceptionType: "System.Exception",
                ExceptionMessage: null,
                StackTrace: null,
                TraceId: "trace-1",
                SpanId: "span-1",
                Attributes: new Dictionary<string, string>()),
        };

        // Act
        await pipeline.ProcessTracesAsync(1, "test-service", "node-1", errorSpans);

        // Assert
        _errorSink.CapturedErrors.Should().HaveCount(1);
        _errorSink.CapturedErrors[0].Message.Should().Contain("Internal error");
    }

    /// <summary>
    /// Tests that ProcessMetricsAsync publishes an ingestion event.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessMetricsAsync_PublishesIngestionEvent()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act
        await pipeline.ProcessMetricsAsync(10, "metrics-service", "node-2");

        // Assert
        _messagePublisher.PublishedMessages.Should().HaveCount(1);
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message.Should().NotBeNull();
        message!.MetricCount.Should().Be(10);
        message.TraceCount.Should().Be(0);
        message.LogCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that ProcessLogsAsync publishes an ingestion event.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessLogsAsync_PublishesIngestionEvent()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act
        await pipeline.ProcessLogsAsync(20, "logging-service", "node-3");

        // Assert
        _messagePublisher.PublishedMessages.Should().HaveCount(1);
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message.Should().NotBeNull();
        message!.LogCount.Should().Be(20);
        message.MetricCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that ProcessAnalyticsEventsAsync routes to PostHog sink.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessAnalyticsEventsAsync_RoutesToPostHogSink()
    {
        // Arrange
        var pipeline = CreatePipeline();

        var events = new List<TelemetryEvent>
        {
            TelemetryEvent.Create("user.signup").WithDistinctId("user-1"),
            TelemetryEvent.Create("feature.used").WithDistinctId("user-1"),
        };

        // Act
        await pipeline.ProcessAnalyticsEventsAsync(events, "app-service", "node-4");

        // Assert
        _analyticsSink.CapturedBatches.Should().HaveCount(1);
        _analyticsSink.CapturedBatches[0].Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that ProcessAnalyticsEventsAsync skips when sink is disabled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessAnalyticsEventsAsync_WhenSinkDisabled_SkipsRouting()
    {
        // Arrange
        var disabledOptions = Options.Create(new PulseCollectorOptions
        {
            EnablePostHogSink = false,
            EnableTransportPublishing = true,
        });

        var pipeline = new IngestionPipeline(
            _enricher,
            _analyticsSink,
            _errorSink,
            traceSinks: [],
            logSinks: [],
            metricsSinks: [],
            _publisher,
            disabledOptions,
            lokiOptions: null,
            NullLogger<IngestionPipeline>.Instance);

        var events = new List<TelemetryEvent>
        {
            TelemetryEvent.Create("test.event").WithDistinctId("user-1"),
        };

        // Act
        await pipeline.ProcessAnalyticsEventsAsync(events, "app-service", "node-4");

        // Assert
        _analyticsSink.CapturedBatches.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ProcessErrorAsync routes to Sentry sink.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessErrorAsync_RoutesToSentrySink()
    {
        // Arrange
        var pipeline = CreatePipeline();

        var errorEvent = ErrorEvent.FromException(new InvalidOperationException("Test error"));

        // Act
        await pipeline.ProcessErrorAsync(errorEvent);

        // Assert
        _errorSink.CapturedErrors.Should().HaveCount(1);
        _errorSink.CapturedErrors[0].Exception.Should().BeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that ProcessErrorAsync skips when Sentry sink is disabled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessErrorAsync_WhenSinkDisabled_SkipsRouting()
    {
        // Arrange
        var disabledOptions = Options.Create(new PulseCollectorOptions
        {
            EnableSentrySink = false,
        });

        var pipeline = new IngestionPipeline(
            _enricher,
            _analyticsSink,
            _errorSink,
            traceSinks: [],
            logSinks: [],
            metricsSinks: [],
            _publisher,
            disabledOptions,
            lokiOptions: null,
            NullLogger<IngestionPipeline>.Instance);

        var errorEvent = ErrorEvent.FromException(new InvalidOperationException("Test"));

        // Act
        await pipeline.ProcessErrorAsync(errorEvent);

        // Assert
        _errorSink.CapturedErrors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that transport publishing is skipped when disabled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WhenTransportDisabled_SkipsPublishing()
    {
        // Arrange
        var disabledOptions = Options.Create(new PulseCollectorOptions
        {
            EnableTransportPublishing = false,
        });

        var pipeline = new IngestionPipeline(
            _enricher,
            _analyticsSink,
            _errorSink,
            traceSinks: [],
            logSinks: [],
            metricsSinks: [],
            _publisher,
            disabledOptions,
            lokiOptions: null,
            NullLogger<IngestionPipeline>.Instance);

        // Act
        await pipeline.ProcessTracesAsync(5, "test-service", "node-1");

        // Assert
        _messagePublisher.PublishedMessages.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that null sinks are handled gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessAnalyticsEventsAsync_WithNullSink_DoesNotThrow()
    {
        // Arrange
        var pipeline = new IngestionPipeline(
            _enricher,
            analyticsSink: null, // null analytics sink
            errorSink: null, // null error sink
            traceSinks: [],
            logSinks: [],
            metricsSinks: [],
            _publisher,
            _options,
            lokiOptions: null,
            NullLogger<IngestionPipeline>.Instance);

        var events = new List<TelemetryEvent>
        {
            TelemetryEvent.Create("test.event").WithDistinctId("user-1"),
        };

        // Act
        var act = () => pipeline.ProcessAnalyticsEventsAsync(events, "app-service", "node-4");

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that error spans with exception details are forwarded correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WithExceptionDetails_ForwardsAllInfo()
    {
        // Arrange
        var pipeline = CreatePipeline();

        var errorSpans = new List<ExtractedErrorSpan>
        {
            new(
                SpanName: "ProcessOrder",
                ServiceName: "test-service",
                ErrorMessage: null,
                ExceptionType: "System.ArgumentException",
                ExceptionMessage: "Value cannot be null",
                StackTrace: "at Test.Method()",
                TraceId: "trace-1",
                SpanId: "span-1",
                Attributes: new Dictionary<string, string>
                {
                    ["http.method"] = "POST",
                    ["http.status_code"] = "500",
                }),
        };

        // Act
        await pipeline.ProcessTracesAsync(1, "test-service", "node-1", errorSpans);

        // Assert
        _errorSink.CapturedErrors.Should().HaveCount(1);
        var captured = _errorSink.CapturedErrors[0];
        captured.Tags.Should().ContainKey("http.method");
        captured.Tags["http.method"].Should().Be("POST");
    }

    /// <summary>
    /// Tests that empty error spans list does not route to Sentry.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_WithEmptyErrorSpans_DoesNotRouteToSentry()
    {
        // Arrange
        var pipeline = CreatePipeline();

        var errorSpans = new List<ExtractedErrorSpan>();

        // Act
        await pipeline.ProcessTracesAsync(5, "test-service", "node-1", errorSpans);

        // Assert
        _errorSink.CapturedErrors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ingestion event has correct batch ID format.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ProcessTracesAsync_IngestionEvent_HasBatchId()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act
        await pipeline.ProcessTracesAsync(1, "test-service", "node-1");

        // Assert
        var message = _messagePublisher.PublishedMessages[0] as PulseIngested;
        message!.BatchId.Should().NotBeNullOrEmpty();
        message.BatchId!.Length.Should().Be(32); // GUID without dashes
    }

    /// <summary>
    /// Creates a new IngestionPipeline instance with the configured test dependencies.
    /// </summary>
    /// <returns>A new IngestionPipeline instance.</returns>
    private IngestionPipeline CreatePipeline()
    {
        return new IngestionPipeline(
            _enricher,
            _analyticsSink,
            _errorSink,
            traceSinks: [],
            logSinks: [],
            metricsSinks: [],
            _publisher,
            _options,
            lokiOptions: null,
            NullLogger<IngestionPipeline>.Instance);
    }

    private sealed class FakeAnalyticsSink : IAnalyticsSink
    {
        public List<List<TelemetryEvent>> CapturedBatches { get; } = [];

        public Task CaptureAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
        {
            CapturedBatches.Add([telemetryEvent]);
            return Task.CompletedTask;
        }

        public Task CaptureBatchAsync(IEnumerable<TelemetryEvent> events, CancellationToken cancellationToken = default)
        {
            CapturedBatches.Add(events.ToList());
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeErrorSink : IErrorSink
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
        public string NodeId => "test-node-1";

        public string Version => "1.0.0";

        public string StudioId => "test-studio";

        public string Environment => "Test";

        public NodeLifecycleStage LifecycleStage { get; private set; } = NodeLifecycleStage.Ready;

        public DateTimeOffset StartedAtUtc => DateTimeOffset.UtcNow;

        public string MachineName => "test-machine";

        public int ProcessId => 12345;

        public IReadOnlyDictionary<string, string> Tags => new Dictionary<string, string>();

        public void SetLifecycleStage(NodeLifecycleStage stage) => LifecycleStage = stage;
    }
}
