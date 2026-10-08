// <copyright file="OtlpAuthenticationExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// Configures fail-closed authentication for the collector's ingestion endpoints.
/// </summary>
public static class OtlpAuthenticationExtensions
{
    /// <summary>
    /// The JWT bearer scheme used exclusively by the ingestion authorization policy.
    /// </summary>
    public const string SchemeName = "PulseOtlpBearer";

    /// <summary>
    /// The authorization policy shared by HTTP and gRPC ingestion routes.
    /// </summary>
    public const string PolicyName = "PulseOtlpIngestion";

    /// <summary>
    /// Validates the ingestion security configuration and registers JWT bearer authentication.
    /// Anonymous ingestion is allowed in Development or with an explicit non-Development opt-out.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="options">The collector options.</param>
    /// <exception cref="InvalidOperationException">
    /// Authentication is required but not configured, or anonymous ingestion is not explicitly allowed.
    /// </exception>
    public static void ConfigureOtlpAuthentication(
        this WebApplicationBuilder builder,
        PulseCollectorOptions options)
    {
        if (!options.RequireOtlpAuthentication)
        {
            if (!builder.Environment.IsDevelopment() && !options.AllowUnauthenticatedOtlpInNonDevelopment)
            {
                throw new InvalidOperationException(
                    $"FAIL-FAST: Anonymous OTLP ingestion is disabled outside Development. " +
                    $"Configure '{PulseCollectorOptions.SectionName}:RequireOtlpAuthentication' with a JWT authority and audience. " +
                    $"Only behind an authenticated, network-isolated gateway may " +
                    $"'{PulseCollectorOptions.SectionName}:AllowUnauthenticatedOtlpInNonDevelopment' be explicitly enabled.");
            }

            return;
        }

        if (!Uri.TryCreate(options.OtlpAuthenticationAuthority, UriKind.Absolute, out var authority)
            || authority.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(authority.Host)
            || !string.IsNullOrEmpty(authority.UserInfo)
            || !string.IsNullOrEmpty(authority.Query)
            || !string.IsNullOrEmpty(authority.Fragment))
        {
            throw new InvalidOperationException(
                $"FAIL-FAST: OTLP authentication requires '{PulseCollectorOptions.SectionName}:OtlpAuthenticationAuthority' " +
                $"to be an absolute HTTPS OpenID Connect authority without credentials, query, or fragment.");
        }

        if (string.IsNullOrWhiteSpace(options.OtlpAuthenticationAudience))
        {
            throw new InvalidOperationException(
                $"FAIL-FAST: OTLP authentication requires '{PulseCollectorOptions.SectionName}:OtlpAuthenticationAudience'. " +
                $"Use an audience dedicated to collector ingestion.");
        }

        builder.Services.AddAuthentication()
            .AddJwtBearer(SchemeName, jwt =>
            {
                jwt.Authority = options.OtlpAuthenticationAuthority;
                jwt.Audience = options.OtlpAuthenticationAudience;
                jwt.RequireHttpsMetadata = true;
                jwt.MapInboundClaims = false;
                jwt.SaveToken = false;
                jwt.IncludeErrorDetails = false;
                jwt.TokenValidationParameters.ValidateIssuer = true;
                jwt.TokenValidationParameters.ValidateAudience = true;
                jwt.TokenValidationParameters.ValidateIssuerSigningKey = true;
                jwt.TokenValidationParameters.RequireSignedTokens = true;
                jwt.TokenValidationParameters.RequireExpirationTime = true;
                jwt.TokenValidationParameters.ValidateLifetime = true;
                jwt.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(PolicyName, policy =>
                policy.AddAuthenticationSchemes(SchemeName).RequireAuthenticatedUser());
    }
}
