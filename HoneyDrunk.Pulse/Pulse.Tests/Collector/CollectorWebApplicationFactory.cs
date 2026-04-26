// <copyright file="CollectorWebApplicationFactory.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Pulse.Collector;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Web application factory for collector endpoint tests.
/// </summary>
/// <remarks>
/// Captures the previous values of process-wide environment variables in the constructor and restores
/// them on Dispose so that tests running in parallel (xUnit collection parallelization) cannot leak
/// state into each other or into the surrounding test host.
/// </remarks>
public class CollectorWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _previousAspNetCoreEnvironment;
    private readonly string? _previousDotNetEnvironment;
    private readonly string? _previousAzureKeyVaultUri;
    private readonly string? _previousAzureAppConfigEndpoint;
    private readonly string? _previousHoneyDrunkNodeId;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectorWebApplicationFactory"/> class.
    /// </summary>
    public CollectorWebApplicationFactory()
    {
        _previousAspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        _previousDotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        _previousAzureKeyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI");
        _previousAzureAppConfigEndpoint = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");
        _previousHoneyDrunkNodeId = Environment.GetEnvironmentVariable("HONEYDRUNK_NODE_ID");

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("AZURE_KEYVAULT_URI", null);
        Environment.SetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT", null);
        Environment.SetEnvironmentVariable("HONEYDRUNK_NODE_ID", null);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _previousAspNetCoreEnvironment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", _previousDotNetEnvironment);
            Environment.SetEnvironmentVariable("AZURE_KEYVAULT_URI", _previousAzureKeyVaultUri);
            Environment.SetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT", _previousAzureAppConfigEndpoint);
            Environment.SetEnvironmentVariable("HONEYDRUNK_NODE_ID", _previousHoneyDrunkNodeId);
        }

        base.Dispose(disposing);
    }
}
