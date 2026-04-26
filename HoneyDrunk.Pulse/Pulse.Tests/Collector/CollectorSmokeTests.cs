// <copyright file="CollectorSmokeTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Smoke tests for the Pulse Collector endpoints.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CollectorSmokeTests"/> class.
/// </remarks>
/// <param name="factory">The web application factory.</param>
public class CollectorSmokeTests(CollectorWebApplicationFactory factory) : IClassFixture<CollectorWebApplicationFactory>
{
    /// <summary>
    /// Verifies that the health endpoint returns OK.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the ready endpoint returns OK.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadyEndpoint_ShouldReturnOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/ready", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the live endpoint returns OK.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LiveEndpoint_ShouldReturnOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/live", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the traces endpoint accepts POST requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TracesEndpoint_ShouldAcceptPost()
    {
        // Arrange
        var client = factory.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/traces", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the metrics endpoint accepts POST requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task MetricsEndpoint_ShouldAcceptPost()
    {
        // Arrange
        var client = factory.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/metrics", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the logs endpoint accepts POST requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LogsEndpoint_ShouldAcceptPost()
    {
        // Arrange
        var client = factory.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/logs", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the analytics endpoint accepts valid requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AnalyticsEndpoint_ShouldAcceptValidRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new
        {
            Events = new[]
            {
                new
                {
                    EventName = "test.event",
                    DistinctId = "user-123",
                },
            },
            SourceService = "TestService",
        };

        // Act
        var response = await client.PostAsJsonAsync("/otlp/v1/analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the analytics endpoint rejects empty events.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AnalyticsEndpoint_ShouldRejectEmptyEvents()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new
        {
            Events = Array.Empty<object>(),
        };

        // Act
        var response = await client.PostAsJsonAsync("/otlp/v1/analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that the errors endpoint accepts valid requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ErrorsEndpoint_ShouldAcceptValidRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new
        {
            Message = "Test error",
            CorrelationId = "corr-123",
        };

        // Act
        var response = await client.PostAsJsonAsync("/otlp/v1/errors", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
