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
    /// <returns>The test task.</returns>
    [Fact]
    public async Task InvalidJson_DoesNotLogInputOrRawExceptions()
    {
        var logger = new CapturingLogger();
        var parser = new OtlpParser(logger);
        var payload = Encoding.UTF8.GetBytes("{\"password=private-sentinel\":{broken}");
        using var traces = new MemoryStream(payload);
        using var logs = new MemoryStream(payload);
        using var metrics = new MemoryStream(payload);

        await parser.ParseTracesAsync(traces, "application/json");
        await parser.ParseLogsAsync(logs, "application/json");
        await parser.ParseMetricsAsync(metrics, "application/json");

        logger.Messages.Should().NotBeEmpty();
        logger.Messages.Should().NotContain(message => message.Contains("private-sentinel", StringComparison.Ordinal));
        logger.Exceptions.Should().OnlyContain(exception => exception == null);
    }

    private sealed class CapturingLogger : ILogger<OtlpParser>
    {
        public List<string> Messages { get; } = [];

        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }
}
