// <copyright file="CollectorWebApplicationFactory.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Pulse.Collector;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Web application factory for collector endpoint tests.
/// </summary>
public class CollectorWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectorWebApplicationFactory"/> class.
    /// </summary>
    public CollectorWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("AZURE_KEYVAULT_URI", null);
        Environment.SetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT", null);
        Environment.SetEnvironmentVariable("HONEYDRUNK_NODE_ID", null);
    }
}
