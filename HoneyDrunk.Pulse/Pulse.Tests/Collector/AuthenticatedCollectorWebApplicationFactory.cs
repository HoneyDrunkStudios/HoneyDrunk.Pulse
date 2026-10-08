// <copyright file="AuthenticatedCollectorWebApplicationFactory.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Pulse.Collector.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Runs the real collector host with JWT validation and in-memory test-only signing keys.
/// </summary>
public sealed class AuthenticatedCollectorWebApplicationFactory : CollectorWebApplicationFactory
{
    private const string Authority = "https://identity.example.com";
    private const string Audience = "pulse-ingestion";
    private const string RequireAuthenticationVariable = "HoneyDrunk__Pulse__Collector__RequireOtlpAuthentication";
    private const string AuthorityVariable = "HoneyDrunk__Pulse__Collector__OtlpAuthenticationAuthority";
    private const string AudienceVariable = "HoneyDrunk__Pulse__Collector__OtlpAuthenticationAudience";
    private readonly RSA _signingKey = RSA.Create(2048);
    private readonly string? _previousRequireAuthentication;
    private readonly string? _previousAuthority;
    private readonly string? _previousAudience;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatedCollectorWebApplicationFactory"/> class.
    /// </summary>
    public AuthenticatedCollectorWebApplicationFactory()
    {
        // Program binds collector options before WebApplicationFactory's host configuration callbacks.
        // Match the base fixture's capture-and-restore approach so authentication is enabled at binding time.
        _previousRequireAuthentication = Environment.GetEnvironmentVariable(RequireAuthenticationVariable);
        _previousAuthority = Environment.GetEnvironmentVariable(AuthorityVariable);
        _previousAudience = Environment.GetEnvironmentVariable(AudienceVariable);
        Environment.SetEnvironmentVariable(RequireAuthenticationVariable, "true");
        Environment.SetEnvironmentVariable(AuthorityVariable, Authority);
        Environment.SetEnvironmentVariable(AudienceVariable, Audience);
    }

    /// <summary>
    /// Creates a test token without using credentials or a network identity provider.
    /// </summary>
    /// <param name="audience">An optional audience override.</param>
    /// <param name="issuer">An optional issuer override.</param>
    /// <param name="expires">An optional expiry override.</param>
    /// <param name="useUntrustedKey">Whether to sign with a key the collector does not trust.</param>
    /// <returns>The serialized test token.</returns>
    public string CreateAccessToken(
        string? audience = null,
        string? issuer = null,
        DateTime? expires = null,
        bool useUntrustedKey = false)
    {
        using var untrustedKey = RSA.Create(2048);
        var key = new RsaSecurityKey(useUntrustedKey ? untrustedKey : _signingKey) { KeyId = "collector-test-key" };
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Authority,
            Audience = audience ?? Audience,
            Subject = new ClaimsIdentity([new Claim("sub", "collector-test-client")]),
            IssuedAt = DateTime.UtcNow.AddMinutes(-10),
            NotBefore = DateTime.UtcNow.AddMinutes(-10),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
        });
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>(
            OtlpAuthenticationExtensions.SchemeName,
            jwt =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Authority };
                configuration.SigningKeys.Add(new RsaSecurityKey(_signingKey.ExportParameters(false))
                {
                    KeyId = "collector-test-key",
                });
                jwt.Configuration = configuration;
                jwt.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }));
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            if (disposing)
            {
                Environment.SetEnvironmentVariable(RequireAuthenticationVariable, _previousRequireAuthentication);
                Environment.SetEnvironmentVariable(AuthorityVariable, _previousAuthority);
                Environment.SetEnvironmentVariable(AudienceVariable, _previousAudience);
                _signingKey.Dispose();
            }
        }
    }
}
