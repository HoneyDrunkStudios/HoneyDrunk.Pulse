// <copyright file="SentrySink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using HoneyDrunk.Telemetry.Sink.Sentry.Options;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Telemetry.Sink.Sentry.Implementation;

/// <summary>
/// Sentry error tracking sink implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SentrySink"/> class.
/// </remarks>
/// <param name="secretStore">The Vault secret store.</param>
/// <param name="options">The Sentry sink options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class SentrySink(
    ISecretStore secretStore,
    IOptions<SentrySinkOptions> options,
    ILogger<SentrySink> logger) : IErrorSink, IDisposable
{
    private readonly SentrySinkOptions _options = options.Value;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private IDisposable? _sentryDisposable;
    private string? _activeDsn;
    private string? _activeDsnVersion;
    private bool _disposed;

    /// <inheritdoc />
    public async Task CaptureAsync(ErrorEvent errorEvent, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_options.Enabled)
        {
            return;
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        SentrySdk.ConfigureScope(scope =>
        {
            ConfigureScope(scope, errorEvent);
        });

        if (errorEvent.Exception is not null)
        {
            SentrySdk.CaptureException(errorEvent.Exception);
        }
        else if (!string.IsNullOrEmpty(errorEvent.Message))
        {
            var sentryLevel = MapSeverityToSentryLevel(errorEvent.Severity);
            SentrySdk.CaptureMessage(errorEvent.Message, sentryLevel);
        }
    }

    /// <inheritdoc />
    public async Task CaptureExceptionAsync(
        Exception exception,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_options.Enabled)
        {
            return;
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        SentrySdk.ConfigureScope(scope =>
        {
            if (tags is not null)
            {
                foreach (var tag in tags)
                {
                    scope.SetTag(tag.Key, tag.Value);
                }
            }

            ApplyDefaultTags(scope);
        });

        SentrySdk.CaptureException(exception);
    }

    /// <inheritdoc />
    public async Task CaptureMessageAsync(
        string message,
        TelemetryEventSeverity severity = TelemetryEventSeverity.Error,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_options.Enabled)
        {
            return;
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        SentrySdk.ConfigureScope(ApplyDefaultTags);

        var sentryLevel = MapSeverityToSentryLevel(severity);
        SentrySdk.CaptureMessage(message, sentryLevel);
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_options.Enabled)
        {
            return;
        }

        await SentrySdk.FlushAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _sentryDisposable?.Dispose();
        _initializationLock.Dispose();
        _disposed = true;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        var secret = await secretStore
            .GetSecretAsync(new SecretIdentifier(_options.DsnSecretName), cancellationToken)
            .ConfigureAwait(false);

        if (string.Equals(_activeDsn, secret.Value, StringComparison.Ordinal)
            && string.Equals(_activeDsnVersion, secret.Version, StringComparison.Ordinal))
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.Equals(_activeDsn, secret.Value, StringComparison.Ordinal)
                && string.Equals(_activeDsnVersion, secret.Version, StringComparison.Ordinal))
            {
                return;
            }

            _sentryDisposable?.Dispose();
            _sentryDisposable = SentrySdk.Init(sentryOptions =>
            {
                sentryOptions.Dsn = secret.Value;
                sentryOptions.Environment = _options.Environment;
                sentryOptions.Release = _options.Release;
                sentryOptions.SampleRate = (float)_options.SampleRate;
                sentryOptions.TracesSampleRate = _options.TracesSampleRate;
                sentryOptions.MaxBreadcrumbs = _options.MaxBreadcrumbs;
                sentryOptions.AttachStacktrace = _options.AttachStacktrace;
                sentryOptions.SendDefaultPii = _options.SendDefaultPii;
            });

            _activeDsn = secret.Value;
            _activeDsnVersion = secret.Version;
            LogSentryInitialized(_options.Environment);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1204:Static elements should appear before instance elements",
        Justification = "Helper kept adjacent to ConfigureScope/CaptureAsync for readability.")]
    private static SentryLevel MapSeverityToSentryLevel(TelemetryEventSeverity severity)
    {
        return severity switch
        {
            TelemetryEventSeverity.Debug => SentryLevel.Debug,
            TelemetryEventSeverity.Info => SentryLevel.Info,
            TelemetryEventSeverity.Warning => SentryLevel.Warning,
            TelemetryEventSeverity.Error => SentryLevel.Error,
            TelemetryEventSeverity.Fatal => SentryLevel.Fatal,
            _ => SentryLevel.Error,
        };
    }

    private void ConfigureScope(Scope scope, ErrorEvent errorEvent)
    {
        // Set correlation tags
        if (!string.IsNullOrEmpty(errorEvent.CorrelationId))
        {
            scope.SetTag(TelemetryTagKeys.HoneyDrunk.CorrelationId, errorEvent.CorrelationId);
        }

        if (!string.IsNullOrEmpty(errorEvent.OperationId))
        {
            scope.SetTag(TelemetryTagKeys.HoneyDrunk.OperationId, errorEvent.OperationId);
        }

        if (!string.IsNullOrEmpty(errorEvent.NodeId))
        {
            scope.SetTag(TelemetryTagKeys.HoneyDrunk.NodeId, errorEvent.NodeId);
        }

        // Set user context
        if (!string.IsNullOrEmpty(errorEvent.UserId))
        {
            scope.User = new SentryUser { Id = errorEvent.UserId };
        }

        // Set custom tags
        foreach (var tag in errorEvent.Tags)
        {
            scope.SetTag(tag.Key, tag.Value);
        }

        // Set extra data
        foreach (var extra in errorEvent.Extra)
        {
            scope.SetExtra(extra.Key, extra.Value);
        }

        // Apply default tags
        ApplyDefaultTags(scope);
    }

    private void ApplyDefaultTags(Scope scope)
    {
        foreach (var tag in _options.DefaultTags)
        {
            scope.SetTag(tag.Key, tag.Value);
        }
    }
}
