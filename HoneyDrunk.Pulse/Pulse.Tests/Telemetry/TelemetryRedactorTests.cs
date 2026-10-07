// <copyright file="TelemetryRedactorTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.OpenTelemetry.Redaction;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Tests default, bounded telemetry redaction and model-copy behavior.
/// </summary>
public sealed class TelemetryRedactorTests
{
    /// <summary>
    /// Sensitive field names are recognized through prefixes, case, and punctuation.
    /// </summary>
    /// <param name="key">A sensitive field name.</param>
    [Theory]
    [InlineData("PASSWORD")]
    [InlineData("user.pass-word")]
    [InlineData("access_token")]
    [InlineData("X-API-KEY")]
    [InlineData("http.request.header.Authorization")]
    [InlineData("Set-Cookie")]
    [InlineData("client_secret")]
    [InlineData("Connection.String")]
    [InlineData("customer.e-mail")]
    [InlineData("Phone_Number")]
    public void RedactValue_SensitiveField_ReplacesEntireValue(string key)
    {
        TelemetryRedactor.IsSensitiveKey(key).Should().BeTrue();
        TelemetryRedactor.RedactValue(key, "sensitive-value").Should().Be(TelemetryRedactor.RedactedValue);
    }

    /// <summary>
    /// Common inline credential formats and contact details are removed without leaking values.
    /// </summary>
    /// <param name="text">Text with a known secret pattern.</param>
    /// <param name="sensitiveValue">The value that must not survive.</param>
    [Theory]
    [InlineData("password=hunter42 done", "hunter42")]
    [InlineData("{\"api-key\":\"hidden value\"}", "hidden value")]
    [InlineData("Authorization: Bearer abc.def.ghi", "abc.def.ghi")]
    [InlineData("Cookie: session=hidden; user=hidden", "hidden")]
    [InlineData("connection_string=Server=local;Password=hidden;", "hidden")]
    [InlineData("Contact somebody@example.test for details", "somebody@example.test")]
    [InlineData("phone: +1 (555) 123-4567", "555")]
    [InlineData("https://user:hidden@example.test/path", "hidden")]
    [InlineData("client_secret=client-value", "client-value")]
    public void RedactText_KnownPattern_RemovesValue(string text, string sensitiveValue)
    {
        var result = TelemetryRedactor.RedactText(text);

        result.Should().NotContain(sensitiveValue);
        result.Should().Contain(TelemetryRedactor.RedactedValue);
        TelemetryRedactor.RedactText(result).Should().Be(result);
    }

    /// <summary>
    /// Basic authentication is redacted using clearly dummy credentials generated only for this test.
    /// </summary>
    [Fact]
    public void RedactText_RuntimeDummyBasicHeader_RemovesEncodedCredentials()
    {
        var dummyCredentials = $"dummy-user-{Guid.NewGuid():N}:dummy-password-{Guid.NewGuid():N}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(dummyCredentials));
        var header = $"Basic {encodedCredentials}";

        var result = TelemetryRedactor.RedactText(header);

        result.Should().Be(TelemetryRedactor.RedactedValue);
        result.Should().NotContain(encodedCredentials);
        TelemetryRedactor.RedactText(result).Should().Be(result);
    }

    /// <summary>
    /// Useful diagnostic text and IDs are preserved; unknown secrets cannot be recognized.
    /// </summary>
    [Fact]
    public void RedactText_BenignOrUnlabeledText_PreservesText()
    {
        TelemetryRedactor.RedactText("request completed for order-42").Should().Be("request completed for order-42");
        TelemetryRedactor.RedactText("unlabeled-opaque-value").Should().Be("unlabeled-opaque-value");
        TelemetryRedactor.RedactText(null).Should().BeNull();
        TelemetryRedactor.RedactText(string.Empty).Should().BeEmpty();
        TelemetryRedactor.IsSensitiveKey("honeydrunk.correlation_id").Should().BeFalse();
        TelemetryRedactor.IsSensitiveKey("trace_id").Should().BeFalse();
    }

    /// <summary>
    /// Read-only dictionaries, lists, and JSON are copied and inspected recursively.
    /// </summary>
    [Fact]
    public void RedactValue_NestedCollectionsAndJson_CopiesWithoutMutating()
    {
        using var json = JsonDocument.Parse("{\"phone\":\"555-1234\",\"count\":2,\"items\":[{\"api_key\":\"json-value\"}]}");
        var inner = new Dictionary<string, object?> { ["password"] = "dictionary-value", ["ok"] = true };
        var original = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>
        {
            ["payload"] = new object?[] { inner, json.RootElement },
            ["correlation_id"] = "business-42",
        });

        var result = TelemetryRedactor.RedactValue("payload", original);
        var serialized = JsonSerializer.Serialize(result);

        serialized.Should().NotContain("dictionary-value").And.NotContain("json-value").And.NotContain("555-1234");
        serialized.Should().Contain("business-42").And.Contain("\"count\":2");
        inner["password"].Should().Be("dictionary-value");
        json.RootElement.GetProperty("phone").GetString().Should().Be("555-1234");
    }

    /// <summary>
    /// Standard homogeneous OTel arrays retain their types and caller-owned storage.
    /// </summary>
    [Fact]
    public void RedactValue_StandardArrays_PreservesTypesAndCopiesValues()
    {
        var original = new long[] { 1, 2, 3 };
        var strings = new[] { "benign", "person@example.test" };

        var numericResult = TelemetryRedactor.RedactValue("durations", original);
        var stringResult = TelemetryRedactor.RedactValue("labels", strings);

        numericResult.Should().BeOfType<long[]>().Which.Should().Equal(original).And.NotBeSameAs(original);
        stringResult.Should().BeOfType<string[]>().Which.Should().Equal("benign", TelemetryRedactor.RedactedValue);
        strings[1].Should().Be("person@example.test");
    }

    /// <summary>
    /// Cyclic, deep, large, and unsupported inputs fail closed instead of escaping or hanging.
    /// </summary>
    [Fact]
    public void RedactValue_ExcessiveOrUnsupportedData_FailsClosed()
    {
        var cycle = new Dictionary<string, object?>();
        cycle["child"] = cycle;

        JsonSerializer.Serialize(TelemetryRedactor.RedactValue("payload", cycle)).Should().Contain(TelemetryRedactor.RedactedValue);
        TelemetryRedactor.RedactValue("payload", Enumerable.Repeat("item", 2000)).Should().Be(TelemetryRedactor.RedactedValue);
        TelemetryRedactor.RedactText(new string('x', 20000)).Should().Be(TelemetryRedactor.RedactedValue);
        TelemetryRedactor.RedactValue("payload", new { Password = "opaque-secret" }).Should().Be(TelemetryRedactor.RedactedValue);
    }

    /// <summary>
    /// Event copies preserve business correlations separately from W3C IDs in the properties.
    /// </summary>
    [Fact]
    public void RedactTelemetryEvent_PreservesIdentifiersAndSanitizesCopy()
    {
        var original = TelemetryEvent.Create("order.completed").WithCorrelationId("business-42");
        original.OperationId = "operation-9";
        original.DistinctId = "somebody@example.test";
        original.Properties["trace_id"] = "1234567890abcdef1234567890abcdef";
        original.Properties["span_id"] = "1234567890abcdef";
        original.Properties["api_key"] = "raw-value";

        var result = TelemetryRedactor.RedactTelemetryEvent(original);

        result.Should().NotBeSameAs(original);
        result.CorrelationId.Should().Be("business-42");
        result.OperationId.Should().Be("operation-9");
        result.Properties["trace_id"].Should().Be(original.Properties["trace_id"]);
        result.Properties["span_id"].Should().Be(original.Properties["span_id"]);
        result.DistinctId.Should().Be(TelemetryRedactor.RedactedValue);
        result.Properties["api_key"].Should().Be(TelemetryRedactor.RedactedValue);
        original.Properties["api_key"].Should().Be("raw-value");
    }

    /// <summary>
    /// Raw exception graphs are not passed through the sanitized error model.
    /// </summary>
    [Fact]
    public void RedactErrorEvent_DropsRawExceptionAndPreservesSafeDetails()
    {
        var exception = new InvalidOperationException("password=hidden", new InvalidOperationException("inner-sensitive"));
        exception.Data["secret"] = "exception-data";
        var original = ErrorEvent.FromException(exception).WithCorrelationId("business-42");
        original.Tags["email"] = "somebody@example.test";
        original.Extra["credentials"] = new Dictionary<string, object?> { ["token"] = "nested-sensitive" };

        var result = TelemetryRedactor.RedactErrorEvent(original);

        result.Exception.Should().BeNull();
        result.Message.Should().NotContain("hidden");
        result.Tags["exception.type"].Should().Be(typeof(InvalidOperationException).FullName);
        result.CorrelationId.Should().Be("business-42");
        JsonSerializer.Serialize(result).Should().NotContain("somebody@example.test").And.NotContain("nested-sensitive").And.NotContain("inner-sensitive").And.NotContain("exception-data");
        original.Exception.Should().BeSameAs(exception);
    }

    /// <summary>
    /// Thrown exceptions preserve sanitized primitive frame metadata in oldest-first order.
    /// </summary>
    [Fact]
    public void RedactErrorEvent_ThrownException_PreservesUsefulSanitizedFrames()
    {
        var exception = CreateThrownException(0);
        exception.Data["private"] = "private-data-sentinel";

        var result = TelemetryRedactor.RedactErrorEvent(ErrorEvent.FromException(exception));
        var frames = result.Extra["exception.stacktrace.frames"].Should()
            .BeOfType<List<Dictionary<string, object?>>>().Which;

        frames.Should().NotBeEmpty();
        frames[0]["function"].Should().Be($"{typeof(TelemetryRedactorTests).FullName}.{nameof(CreateThrownException)}");
        frames[^1]["function"].Should().Be($"{typeof(TelemetryRedactorTests).FullName}.{nameof(ThrowAtLeaf)}");
        frames.SelectMany(frame => frame.Values).Should().OnlyContain(value => value is string || value is int);
        frames.SelectMany(frame => frame.Where(field => field.Key is "lineno" or "colno"))
            .Should().OnlyContain(field => field.Value is int && (int)field.Value > 0);
        JsonSerializer.Serialize(result).Should().NotContain("outer-sensitive").And.NotContain("inner-private-sentinel").And.NotContain("private-data-sentinel");
        result.Exception.Should().BeNull();
    }

    /// <summary>
    /// Deep exception stacks produce at most 128 frames while retaining the throwing method.
    /// </summary>
    [Fact]
    public void RedactErrorEvent_DeepException_BoundsFrameMetadata()
    {
        var result = TelemetryRedactor.RedactErrorEvent(ErrorEvent.FromException(CreateThrownException(160)));
        var frames = result.Extra["exception.stacktrace.frames"].Should()
            .BeOfType<List<Dictionary<string, object?>>>().Which;

        frames.Should().HaveCount(128);
        frames[^1]["function"].Should().Be($"{typeof(TelemetryRedactorTests).FullName}.{nameof(ThrowAtLeaf)}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InvalidOperationException CreateThrownException(int depth)
    {
        try
        {
            ThrowWithStackTrace(depth);
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The test helper must throw.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowWithStackTrace(int depth)
    {
        if (depth == 0)
        {
            ThrowAtLeaf();
        }
        else
        {
            ThrowWithStackTrace(depth - 1);
        }

        // Prevent tail-call elimination from hiding the bounded-stack test's frames.
        GC.KeepAlive(depth);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowAtLeaf()
        => throw new InvalidOperationException("password=outer-sensitive", new InvalidOperationException("inner-private-sentinel"));
}
