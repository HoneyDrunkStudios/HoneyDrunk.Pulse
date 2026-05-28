// <copyright file="OtlpEndpointValidationTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Pulse.Collector.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Tests for OTLP endpoint validation behavior in different environments.
/// </summary>
public class OtlpEndpointValidationTests
{
    /// <summary>
    /// In Development, missing OTLP endpoint should return localhost default.
    /// </summary>
    [Fact]
    public void GetValidatedOtlpEndpoint_Development_MissingEndpoint_ReturnsLocalhostDefault()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);

        // Act
        var endpoint = builder.GetValidatedOtlpEndpoint();

        // Assert
        endpoint.Should().Be(OtlpEndpointValidationExtensions.LocalhostDefault);
    }

    /// <summary>
    /// In Development, configured localhost endpoint should be allowed.
    /// </summary>
    [Fact]
    public void GetValidatedOtlpEndpoint_Development_LocalhostEndpoint_ReturnsConfiguredValue()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [OtlpEndpointValidationExtensions.OtlpEndpointConfigKey] = "http://localhost:4318",
        });

        // Act
        var endpoint = builder.GetValidatedOtlpEndpoint();

        // Assert
        endpoint.Should().Be("http://localhost:4318");
    }

    /// <summary>
    /// In Development, configured remote endpoint should be allowed.
    /// </summary>
    [Fact]
    public void GetValidatedOtlpEndpoint_Development_RemoteEndpoint_ReturnsConfiguredValue()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [OtlpEndpointValidationExtensions.OtlpEndpointConfigKey] = "https://otel.example.com:4317",
        });

        // Act
        var endpoint = builder.GetValidatedOtlpEndpoint();

        // Assert
        endpoint.Should().Be("https://otel.example.com:4317");
    }

    /// <summary>
    /// In Production, missing OTLP endpoint should throw.
    /// </summary>
    [Fact]
    public void GetValidatedOtlpEndpoint_Production_MissingEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange - Create builder without default configuration loading
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production",
        });

        // Act
        var act = () => builder.GetValidatedOtlpEndpoint();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{OtlpEndpointValidationExtensions.OtlpEndpointConfigKey}*")
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*required*");
    }

    /// <summary>
    /// In Production, empty OTLP endpoint should throw.
    /// </summary>
    [Fact]
    public void GetValidatedOtlpEndpoint_Production_EmptyEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange - Create builder without default configuration loading
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production",
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [OtlpEndpointValidationExtensions.OtlpEndpointConfigKey] = "   ",
        });

        // Act
        var act = () => builder.GetValidatedOtlpEndpoint();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{OtlpEndpointValidationExtensions.OtlpEndpointConfigKey}*")
            .WithMessage("*FAIL-FAST*");
    }

    /// <summary>
    /// In Production, localhost endpoint should throw.
    /// </summary>
    /// <param name="localhostEndpoint">The localhost endpoint to test.</param>
    [Theory]
    [InlineData("http://localhost:4317")]
    [InlineData("http://127.0.0.1:4317")]
    [InlineData("http://[::1]:4317")]
    [InlineData("http://0.0.0.0:4317")]
    public void GetValidatedOtlpEndpoint_Production_LocalhostEndpoint_ThrowsInvalidOperationException(string localhostEndpoint)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [OtlpEndpointValidationExtensions.OtlpEndpointConfigKey] = localhostEndpoint,
        });

        // Act
        var act = () => builder.GetValidatedOtlpEndpoint();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{OtlpEndpointValidationExtensions.OtlpEndpointConfigKey}*")
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*localhost*");
    }

    /// <summary>
    /// In Production, invalid URI should throw with helpful message.
    /// </summary>
    [Fact]
    public void GetValidatedOtlpEndpoint_Production_InvalidUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [OtlpEndpointValidationExtensions.OtlpEndpointConfigKey] = "not-a-valid-uri",
        });

        // Act
        var act = () => builder.GetValidatedOtlpEndpoint();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{OtlpEndpointValidationExtensions.OtlpEndpointConfigKey}*")
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*valid absolute URI*");
    }

    /// <summary>
    /// In Production, valid remote endpoint should be accepted.
    /// </summary>
    /// <param name="remoteEndpoint">The remote endpoint to test.</param>
    [Theory]
    [InlineData("https://otel-collector.example.com:4317")]
    [InlineData("http://otel.internal.svc.cluster.local:4317")]
    [InlineData("grpc://telemetry.corp.net:443")]
    public void GetValidatedOtlpEndpoint_Production_RemoteEndpoint_ReturnsConfiguredValue(string remoteEndpoint)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [OtlpEndpointValidationExtensions.OtlpEndpointConfigKey] = remoteEndpoint,
        });

        // Act
        var endpoint = builder.GetValidatedOtlpEndpoint();

        // Assert
        endpoint.Should().Be(remoteEndpoint);
    }

    /// <summary>
    /// In Staging, localhost endpoint should throw (same as Production).
    /// </summary>
    [Fact]
    public void GetValidatedOtlpEndpoint_Staging_LocalhostEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Staging"]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [OtlpEndpointValidationExtensions.OtlpEndpointConfigKey] = "http://localhost:4317",
        });

        // Act
        var act = () => builder.GetValidatedOtlpEndpoint();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*localhost*");
    }

    /// <summary>
    /// Self-reference check is a no-op when no endpoint is configured.
    /// </summary>
    [Fact]
    public void ValidateOtlpEndpointNotSelfReferencing_NullOrEmptyEndpoint_ReturnsAppUnchanged()
    {
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        var app = builder.Build();

        var result1 = app.ValidateOtlpEndpointNotSelfReferencing(null);
        var result2 = app.ValidateOtlpEndpointNotSelfReferencing(string.Empty);

        result1.Should().BeSameAs(app);
        result2.Should().BeSameAs(app);
    }

    /// <summary>
    /// Invalid (non-absolute) URI is logged and skipped, not thrown.
    /// </summary>
    [Fact]
    public void ValidateOtlpEndpointNotSelfReferencing_InvalidUri_ReturnsAppUnchanged()
    {
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        var app = builder.Build();

        var result = app.ValidateOtlpEndpointNotSelfReferencing("not-a-valid-uri");

        result.Should().BeSameAs(app);
    }

    /// <summary>
    /// Remote endpoint pointing somewhere other than the collector returns unchanged.
    /// </summary>
    [Fact]
    public void ValidateOtlpEndpointNotSelfReferencing_RemoteEndpoint_ReturnsAppUnchanged()
    {
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var app = builder.Build();

        var result = app.ValidateOtlpEndpointNotSelfReferencing("https://otel-collector.example.com:4317");

        result.Should().BeSameAs(app);
    }

    /// <summary>
    /// Endpoint pointing to one of the app's own listening URLs throws fail-fast.
    /// </summary>
    [Fact]
    public void ValidateOtlpEndpointNotSelfReferencing_SameHostSamePort_ThrowsInvalidOperationException()
    {
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var app = builder.Build();
        app.Urls.Add("http://localhost:5000");

        var act = () => app.ValidateOtlpEndpointNotSelfReferencing("http://localhost:5000");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*infinite loop*");
    }

    /// <summary>
    /// Endpoint pointing to a localhost variant of the listening URL on the same port is still self-referencing.
    /// </summary>
    [Fact]
    public void ValidateOtlpEndpointNotSelfReferencing_DifferentLocalhostVariantSamePort_ThrowsInvalidOperationException()
    {
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:5000");

        var act = () => app.ValidateOtlpEndpointNotSelfReferencing("http://localhost:5000");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*infinite loop*");
    }

    /// <summary>
    /// Same host but different port is NOT self-referencing.
    /// </summary>
    [Fact]
    public void ValidateOtlpEndpointNotSelfReferencing_SameHostDifferentPort_ReturnsAppUnchanged()
    {
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var app = builder.Build();
        app.Urls.Add("http://localhost:5000");

        var result = app.ValidateOtlpEndpointNotSelfReferencing("http://localhost:4317");

        result.Should().BeSameAs(app);
    }
}
