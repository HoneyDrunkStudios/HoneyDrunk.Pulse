// <copyright file="HttpOtlpSinkAuthSecretNames.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Shared;

/// <summary>
/// Vault secret names that resolve to HTTP authentication material at export time.
/// Bundled to keep <see cref="HttpOtlpSinkOptionsAdapter"/> within the parameter-count limit (Sonar S107).
/// </summary>
/// <param name="BasicAuthSecretName">Vault secret name for a pre-encoded Authorization header value.</param>
/// <param name="UsernameSecretName">Vault secret name for the basic auth username.</param>
/// <param name="PasswordSecretName">Vault secret name for the basic auth password.</param>
internal sealed record HttpOtlpSinkAuthSecretNames(
    string BasicAuthSecretName,
    string UsernameSecretName,
    string PasswordSecretName);
