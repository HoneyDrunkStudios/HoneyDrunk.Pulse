// <copyright file="TransportPublishingIntegrationTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Pulse.Contracts.Events;
using HoneyDrunk.Pulse.Tests.Collector;
using HoneyDrunk.Transport.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace HoneyDrunk.Pulse.Tests.Transport;

/// <summary>
/// Integration tests verifying that Transport events are published when telemetry is ingested.
/// </summary>
public class TransportPublishingIntegrationTests : IClassFixture<TransportPublishingIntegrationTests.CollectorWithCapturingPublisher>
{
    private readonly CollectorWithCapturingPublisher _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransportPublishingIntegrationTests"/> class.
    /// </summary>
    /// <param name="factory">The custom web application factory with message capturing.</param>
    public TransportPublishingIntegrationTests(CollectorWithCapturingPublisher factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that posting traces via HTTP publishes a PulseIngested event through Transport.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PostTraces_ShouldPublishPulseIngestedEvent()
    {
        // Arrange
        var client = _factory.CreateClient();
        _factory.CapturedPublisher.PublishedMessages.Clear();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/traces", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.CapturedPublisher.PublishedMessages.Should().HaveCount(1);
        var published = _factory.CapturedPublisher.PublishedMessages.First();
        published.Should().BeOfType<PulseIngested>();

        var pulseIngested = (PulseIngested)published;
        pulseIngested.TraceCount.Should().BeGreaterThanOrEqualTo(0);
        pulseIngested.Status.Should().Be(IngestionStatus.Success);
    }

    /// <summary>
    /// Verifies that posting metrics via HTTP publishes a PulseIngested event through Transport.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PostMetrics_ShouldPublishPulseIngestedEvent()
    {
        // Arrange
        var client = _factory.CreateClient();
        _factory.CapturedPublisher.PublishedMessages.Clear();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/metrics", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.CapturedPublisher.PublishedMessages.Should().HaveCount(1);
        var published = _factory.CapturedPublisher.PublishedMessages.First();
        published.Should().BeOfType<PulseIngested>();

        var pulseIngested = (PulseIngested)published;
        pulseIngested.MetricCount.Should().BeGreaterThanOrEqualTo(0);
        pulseIngested.Status.Should().Be(IngestionStatus.Success);
    }

    /// <summary>
    /// Verifies that posting logs via HTTP publishes a PulseIngested event through Transport.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PostLogs_ShouldPublishPulseIngestedEvent()
    {
        // Arrange
        var client = _factory.CreateClient();
        _factory.CapturedPublisher.PublishedMessages.Clear();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/logs", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.CapturedPublisher.PublishedMessages.Should().HaveCount(1);
        var published = _factory.CapturedPublisher.PublishedMessages.First();
        published.Should().BeOfType<PulseIngested>();

        var pulseIngested = (PulseIngested)published;
        pulseIngested.LogCount.Should().BeGreaterThanOrEqualTo(0);
        pulseIngested.Status.Should().Be(IngestionStatus.Success);
    }

    /// <summary>
    /// Verifies that the PulseIngested event contains correct metadata.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PostTraces_PulseIngestedShouldContainCorrectMetadata()
    {
        // Arrange
        var client = _factory.CreateClient();
        _factory.CapturedPublisher.PublishedMessages.Clear();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Add("X-Source-Service", "test-service");
        client.DefaultRequestHeaders.Add("X-Source-NodeId", "test-node-123");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/traces", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = _factory.CapturedPublisher.PublishedMessages.First();
        var pulseIngested = published.Should().BeOfType<PulseIngested>().Subject;

        pulseIngested.SourceNodeName.Should().Be("test-service");
        pulseIngested.SourceNodeId.Should().Be("test-node-123");
        pulseIngested.BatchId.Should().NotBeNullOrEmpty();
        pulseIngested.ProcessingDurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Custom WebApplicationFactory that replaces IMessagePublisher with a capturing implementation.
    /// </summary>
    public sealed class CollectorWithCapturingPublisher : CollectorWebApplicationFactory
    {
        /// <summary>
        /// Gets the captured publisher that records all published messages.
        /// </summary>
        public CapturingMessagePublisher CapturedPublisher { get; } = new();

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real IMessagePublisher and replace with our capturing implementation
                services.RemoveAll<IMessagePublisher>();
                services.AddSingleton<IMessagePublisher>(CapturedPublisher);
            });
        }
    }

    /// <summary>
    /// A message publisher that captures all published messages for test verification.
    /// </summary>
    public sealed class CapturingMessagePublisher : IMessagePublisher
    {
        /// <summary>
        /// Gets the list of captured messages.
        /// </summary>
        public List<object> PublishedMessages { get; } = [];

        /// <inheritdoc/>
        public Task PublishAsync<T>(string destination, T message, IGridContext gridContext, CancellationToken cancellationToken = default)
            where T : class
        {
            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
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
}
