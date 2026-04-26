// <copyright file="PostHogSinkTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Sink.PostHog.Implementation;
using HoneyDrunk.Telemetry.Sink.PostHog.Options;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Tests for the PostHogSink class.
/// </summary>
public sealed class PostHogSinkTests : IDisposable
{
    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly TestSecretStore _secretStore = new("PostHog--ApiKey", "test-key");

    /// <summary>
    /// Initializes a new instance of the <see cref="PostHogSinkTests"/> class.
    /// </summary>
    public PostHogSinkTests()
    {
        _httpHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("https://app.posthog.com"),
        };
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        _httpHandler.Dispose();
    }

    /// <summary>
    /// Tests that CaptureAsync skips when sink is disabled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CaptureAsync_WhenDisabled_SkipsSending()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = false,
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);
        var telemetryEvent = TelemetryEvent.Create("test.event").WithDistinctId("user-1");

        // Act
        await sink.CaptureAsync(telemetryEvent);
        await sink.FlushAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that FlushAsync sends pending events.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task FlushAsync_SendsPendingEvents()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = true,
            Host = "https://app.posthog.com",
            BatchSize = 100, // High batch size to prevent auto-flush
            FlushIntervalMs = 0, // Disable auto-flush timer
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);

        await sink.CaptureAsync(TelemetryEvent.Create("event1").WithDistinctId("user-1"));
        await sink.CaptureAsync(TelemetryEvent.Create("event2").WithDistinctId("user-1"));

        // Act
        await sink.FlushAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(1);
        _httpHandler.LastRequestBody.Should().Contain("event1");
        _httpHandler.LastRequestBody.Should().Contain("event2");
    }

    /// <summary>
    /// Tests that flush resolves the PostHog API key from Vault for each send.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task FlushAsync_AfterApiKeyRotation_UsesLatestVaultSecret()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = true,
            Host = "https://app.posthog.com",
            BatchSize = 100,
            FlushIntervalMs = 0,
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);

        await sink.CaptureAsync(TelemetryEvent.Create("event1").WithDistinctId("user-1"));
        await sink.FlushAsync();

        _secretStore.Set("rotated-key", "v2");
        await sink.CaptureAsync(TelemetryEvent.Create("event2").WithDistinctId("user-1"));

        // Act
        await sink.FlushAsync();

        // Assert
        _httpHandler.LastRequestBody.Should().Contain("\"api_key\":\"rotated-key\"");
    }

    /// <summary>
    /// Tests that auto-flush triggers when batch size is reached.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CaptureAsync_WhenBatchSizeReached_AutoFlushes()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = true,
            Host = "https://app.posthog.com",
            BatchSize = 2, // Small batch size
            FlushIntervalMs = 0,
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);

        // Act - Add 2 events to trigger auto-flush
        await sink.CaptureAsync(TelemetryEvent.Create("event1").WithDistinctId("user-1"));
        await sink.CaptureAsync(TelemetryEvent.Create("event2").WithDistinctId("user-1"));

        // Assert
        _httpHandler.RequestCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that CaptureBatchAsync adds all events.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CaptureBatchAsync_AddsAllEvents()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = true,
            Host = "https://app.posthog.com",
            BatchSize = 100,
            FlushIntervalMs = 0,
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);

        var events = new List<TelemetryEvent>
        {
            TelemetryEvent.Create("event1").WithDistinctId("user-1"),
            TelemetryEvent.Create("event2").WithDistinctId("user-2"),
            TelemetryEvent.Create("event3").WithDistinctId("user-3"),
        };

        // Act
        await sink.CaptureBatchAsync(events);
        await sink.FlushAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(1);
        _httpHandler.LastRequestBody.Should().Contain("event1");
        _httpHandler.LastRequestBody.Should().Contain("event2");
        _httpHandler.LastRequestBody.Should().Contain("event3");
    }

    /// <summary>
    /// Tests that CaptureBatchAsync when disabled skips sending.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CaptureBatchAsync_WhenDisabled_SkipsSending()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = false,
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);

        var events = new List<TelemetryEvent>
        {
            TelemetryEvent.Create("event1").WithDistinctId("user-1"),
        };

        // Act
        await sink.CaptureBatchAsync(events);
        await sink.FlushAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that FlushAsync with empty queue does not send.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task FlushAsync_WithEmptyQueue_DoesNotSend()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = true,
            Host = "https://app.posthog.com",
            FlushIntervalMs = 0,
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);

        // Act
        await sink.FlushAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that options validation fails when enabled without API key secret name.
    /// </summary>
    [Fact]
    public void PostHogSinkOptions_ShouldValidate_WhenEnabledWithoutApiKeySecretName()
    {
        // Arrange
        var options = new PostHogSinkOptions
        {
            Enabled = true,
            ApiKeySecretName = string.Empty,
        };

        // Act
        var action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key*required*");
    }

    /// <summary>
    /// Tests that options validation succeeds when disabled without API key secret name.
    /// </summary>
    [Fact]
    public void PostHogSinkOptions_ShouldNotThrow_WhenDisabledWithoutApiKeySecretName()
    {
        // Arrange
        var options = new PostHogSinkOptions
        {
            Enabled = false,
            ApiKeySecretName = string.Empty,
        };

        // Act
        var action = options.Validate;

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new PostHogSinkOptions
        {
            Enabled = true,
            FlushIntervalMs = 0,
        });

        using var sink = new PostHogSink(_httpClient, _secretStore, options, NullLogger<PostHogSink>.Instance);

        // Act
        var action = () =>
        {
            sink.Dispose();
            sink.Dispose();
        };

        // Assert
        action.Should().NotThrow();
    }

    private sealed class TestSecretStore(string name, string initialValue) : ISecretStore
    {
        private string _value = initialValue;
        private string _version = "v1";

        public void Set(string value, string version)
        {
            _value = value;
            _version = version;
        }

        public Task<SecretValue> GetSecretAsync(
            SecretIdentifier identifier,
            CancellationToken cancellationToken = default)
        {
            if (identifier.Name == name)
            {
                return Task.FromResult(new SecretValue(identifier, _value, _version));
            }

            throw new KeyNotFoundException(identifier.Name);
        }

        public Task<VaultResult<SecretValue>> TryGetSecretAsync(
            SecretIdentifier identifier,
            CancellationToken cancellationToken = default)
        {
            if (identifier.Name == name)
            {
                return Task.FromResult(VaultResult.Success(new SecretValue(identifier, _value, _version)));
            }

            return Task.FromResult(VaultResult.Failure<SecretValue>("Not found"));
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
            string secretName,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SecretVersion> versions = secretName == name
                ? [new SecretVersion(_version, DateTimeOffset.UtcNow)]
                : [];

            return Task.FromResult(versions);
        }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public string? LastRequestBody { get; private set; }

        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(ResponseStatusCode)
            {
                Content = new StringContent("{}"),
            };
        }
    }
}
