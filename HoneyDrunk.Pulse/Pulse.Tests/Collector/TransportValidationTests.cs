// <copyright file="TransportValidationTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Pulse.Collector.Configuration;
using Microsoft.AspNetCore.Builder;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Tests for Transport adapter validation behavior in different environments.
/// </summary>
public class TransportValidationTests
{
    /// <summary>
    /// In Development, InMemory Transport should be allowed.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Development_InMemory_Succeeds()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        var options = new PulseCollectorOptions { TransportAdapter = "InMemory" };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// In Development, missing transport adapter should default to InMemory.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Development_MissingAdapter_DefaultsToInMemory()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        var options = new PulseCollectorOptions { TransportAdapter = null! };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// In Development, empty transport adapter should default to InMemory.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Development_EmptyAdapter_DefaultsToInMemory()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        var options = new PulseCollectorOptions { TransportAdapter = "   " };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// In Production, InMemory Transport should throw.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Production_InMemory_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions { TransportAdapter = "InMemory" };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*InMemory*")
            .WithMessage("*not permitted*")
            .WithMessage("*non-Development*");
    }

    /// <summary>
    /// In Production, missing transport adapter should throw.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Production_MissingAdapter_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions { TransportAdapter = null! };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*not configured*")
            .WithMessage($"*{PulseCollectorOptions.SectionName}:TransportAdapter*");
    }

    /// <summary>
    /// In Production, empty transport adapter should throw.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Production_EmptyAdapter_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions { TransportAdapter = "   " };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*not configured*");
    }

    /// <summary>
    /// In Staging, InMemory Transport should throw (same as Production).
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Staging_InMemory_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Staging"]);
        var options = new PulseCollectorOptions { TransportAdapter = "InMemory" };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*InMemory*")
            .WithMessage("*not permitted*");
    }

    /// <summary>
    /// In Production, unknown transport adapter should throw with helpful message.
    /// </summary>
    /// <param name="adapter">The transport adapter name to test.</param>
    [Theory]
    [InlineData("RabbitMQ")]
    [InlineData("Kafka")]
    [InlineData("UnknownAdapter")]
    public void ConfigureTransportAdapter_Production_UnknownAdapter_ThrowsInvalidOperationException(string adapter)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions { TransportAdapter = adapter };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage($"*{adapter}*")
            .WithMessage("*Unknown*");
    }

    /// <summary>
    /// In Production, Azure Service Bus without queue/topic name should throw.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Production_AzureServiceBus_MissingQueueName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions
        {
            TransportAdapter = "AzureServiceBus",
            ServiceBusQueueOrTopicName = null,
        };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*queue or topic name*")
            .WithMessage("*not configured*");
    }

    /// <summary>
    /// In Production, Azure Service Bus with empty queue/topic name should throw.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Production_AzureServiceBus_EmptyQueueName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions
        {
            TransportAdapter = "AzureServiceBus",
            ServiceBusQueueOrTopicName = "   ",
        };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*")
            .WithMessage("*queue or topic name*")
            .WithMessage("*not configured*");
    }

    /// <summary>
    /// Azure Service Bus adapter comparison should be case-insensitive.
    /// </summary>
    /// <param name="adapter">The transport adapter name to test.</param>
    [Theory]
    [InlineData("azureservicebus")]
    [InlineData("AZURESERVICEBUS")]
    [InlineData("AzureServiceBus")]
    [InlineData("azureSERVICEbus")]
    public void ConfigureTransportAdapter_AzureServiceBusCaseInsensitive_ValidatesQueueName(string adapter)
    {
        // Arrange - missing queue name should fail regardless of case
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions
        {
            TransportAdapter = adapter,
            ServiceBusQueueOrTopicName = null,
        };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert - should reach the queue/topic validation (proving case-insensitivity)
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*queue or topic name*");
    }

    /// <summary>
    /// InMemory adapter comparison should be case-insensitive.
    /// </summary>
    /// <param name="adapter">The transport adapter name to test.</param>
    [Theory]
    [InlineData("inmemory")]
    [InlineData("INMEMORY")]
    [InlineData("InMemory")]
    [InlineData("inMEMORY")]
    public void ConfigureTransportAdapter_Development_InMemoryCaseInsensitive_Succeeds(string adapter)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Development"]);
        var options = new PulseCollectorOptions { TransportAdapter = adapter };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Error message should include current environment name.
    /// </summary>
    [Fact]
    public void ConfigureTransportAdapter_Production_InMemory_ErrorIncludesEnvironmentName()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(["--environment", "Production"]);
        var options = new PulseCollectorOptions { TransportAdapter = "InMemory" };

        // Act
        var act = () => builder.ConfigureTransportAdapter(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Production*");
    }
}
