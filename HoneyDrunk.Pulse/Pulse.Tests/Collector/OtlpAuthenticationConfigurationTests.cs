// <copyright file="OtlpAuthenticationConfigurationTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Pulse.Collector.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Tests for fail-closed ingestion authentication configuration.
/// </summary>
public class OtlpAuthenticationConfigurationTests
{
    /// <summary>
    /// Local development retains anonymous ingestion by default.
    /// </summary>
    [Fact]
    public void ConfigureOtlpAuthentication_Development_AllowsAnonymousByDefault()
    {
        var builder = CreateBuilder("Development");

        var act = () => builder.ConfigureOtlpAuthentication(new PulseCollectorOptions());

        act.Should().NotThrow();
    }

    /// <summary>
    /// Every non-Development environment requires a deliberate security configuration.
    /// </summary>
    /// <param name="environment">The host environment name.</param>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("dev")]
    public void ConfigureOtlpAuthentication_NonDevelopment_RejectsAnonymousByDefault(string environment)
    {
        var builder = CreateBuilder(environment);

        var act = () => builder.ConfigureOtlpAuthentication(new PulseCollectorOptions());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*AllowUnauthenticatedOtlpInNonDevelopment*");
    }

    /// <summary>
    /// A network-isolated deployment can explicitly opt into externally authenticated ingestion.
    /// </summary>
    [Fact]
    public void ConfigureOtlpAuthentication_Production_AllowsExplicitAnonymousOptOut()
    {
        var builder = CreateBuilder("Production");
        var options = new PulseCollectorOptions { AllowUnauthenticatedOtlpInNonDevelopment = true };

        var act = () => builder.ConfigureOtlpAuthentication(options);

        act.Should().NotThrow();
    }

    /// <summary>
    /// Neither Development nor an anonymous opt-out can bypass explicitly requested authentication.
    /// </summary>
    /// <param name="environment">The host environment name.</param>
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void ConfigureOtlpAuthentication_RequiredWithoutConfiguration_Throws(string environment)
    {
        var builder = CreateBuilder(environment);
        var options = new PulseCollectorOptions
        {
            RequireOtlpAuthentication = true,
            AllowUnauthenticatedOtlpInNonDevelopment = true,
        };

        var act = () => builder.ConfigureOtlpAuthentication(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FAIL-FAST*OtlpAuthenticationAuthority*");
    }

    /// <summary>
    /// Authority discovery must use a valid HTTPS URL without embedded credentials or extra components.
    /// </summary>
    /// <param name="authority">The invalid authority value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-uri")]
    [InlineData("http://identity.example.com")]
    [InlineData("https://user:password@identity.example.com")]
    [InlineData("https://identity.example.com?key=value")]
    [InlineData("https://identity.example.com#fragment")]
    public void ConfigureOtlpAuthentication_InvalidAuthority_Throws(string? authority)
    {
        var builder = CreateBuilder("Production");
        var options = new PulseCollectorOptions
        {
            RequireOtlpAuthentication = true,
            OtlpAuthenticationAuthority = authority,
            OtlpAuthenticationAudience = "pulse-ingestion",
        };

        var act = () => builder.ConfigureOtlpAuthentication(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpAuthenticationAuthority*");
    }

    /// <summary>
    /// A valid authority cannot accidentally authorize tokens for unrelated audiences.
    /// </summary>
    /// <param name="audience">The missing audience value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConfigureOtlpAuthentication_MissingAudience_Throws(string? audience)
    {
        var builder = CreateBuilder("Production");
        var options = new PulseCollectorOptions
        {
            RequireOtlpAuthentication = true,
            OtlpAuthenticationAuthority = "https://identity.example.com",
            OtlpAuthenticationAudience = audience,
        };

        var act = () => builder.ConfigureOtlpAuthentication(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpAuthenticationAudience*");
    }

    /// <summary>
    /// Secure configuration registers strict token validation and a scheme-specific policy.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConfigureOtlpAuthentication_ValidConfiguration_RegistersStrictValidation()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        var options = new PulseCollectorOptions
        {
            RequireOtlpAuthentication = true,
            OtlpAuthenticationAuthority = "https://identity.example.com",
            OtlpAuthenticationAudience = "pulse-ingestion",
        };
        builder.ConfigureOtlpAuthentication(options);
        await using var app = builder.Build();

        var jwt = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(OtlpAuthenticationExtensions.SchemeName);
        var policy = await app.Services.GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync(OtlpAuthenticationExtensions.PolicyName)
            ?? throw new InvalidOperationException("The ingestion authorization policy was not registered.");

        jwt.Authority.Should().Be(options.OtlpAuthenticationAuthority);
        jwt.Audience.Should().Be(options.OtlpAuthenticationAudience);
        jwt.RequireHttpsMetadata.Should().BeTrue();
        jwt.SaveToken.Should().BeFalse();
        jwt.IncludeErrorDetails.Should().BeFalse();
        jwt.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwt.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwt.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwt.TokenValidationParameters.RequireSignedTokens.Should().BeTrue();
        jwt.TokenValidationParameters.RequireExpirationTime.Should().BeTrue();
        jwt.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        policy.AuthenticationSchemes.Should().ContainSingle().Which.Should().Be(OtlpAuthenticationExtensions.SchemeName);
    }

    private static WebApplicationBuilder CreateBuilder(string environment)
        => WebApplication.CreateEmptyBuilder(new WebApplicationOptions { EnvironmentName = environment });
}
