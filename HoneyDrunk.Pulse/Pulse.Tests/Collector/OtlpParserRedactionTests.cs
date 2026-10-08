// <copyright file="OtlpParserRedactionTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Pulse.Collector.Ingestion;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>Malformed input diagnostics never attach payload-bearing parser exceptions.</summary>
public sealed class OtlpParserRedactionTests
{
    /// <summary>All OTLP parsers restrict local failure diagnostics to exception types.</summary>
    /// <param name="loggingEnabled">Whether diagnostics are enabled.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidJson_DoesNotLogInputOrRawExceptions(bool loggingEnabled)
    {
        var logger = new CapturingLogger(loggingEnabled);
        var parser = new OtlpParser(logger);
        var payload = Encoding.UTF8.GetBytes("{\"password=private-sentinel\":{broken}");
        using var traces = new MemoryStream(payload);
        using var logs = new MemoryStream(payload);
        using var metrics = new MemoryStream(payload);

        await parser.ParseTracesAsync(traces, "application/json");
        await parser.ParseLogsAsync(logs, "application/json");
        await parser.ParseMetricsAsync(metrics, "application/json");

        if (loggingEnabled)
        {
            logger.Messages.Should().NotBeEmpty();
            logger.Messages.Should().NotContain(message => message.Contains("private-sentinel", StringComparison.Ordinal));
            logger.Exceptions.Should().OnlyContain(exception => exception == null);
        }
        else
        {
            logger.Messages.Should().BeEmpty();
            logger.Exceptions.Should().BeEmpty();
        }
    }

    private sealed class CapturingLogger(bool loggingEnabled) : ILogger<OtlpParser>
    {
        public List<string> Messages { get; } = [];

        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => loggingEnabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }
}
