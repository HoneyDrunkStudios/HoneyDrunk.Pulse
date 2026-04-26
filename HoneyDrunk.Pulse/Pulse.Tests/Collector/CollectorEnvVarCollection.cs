// <copyright file="CollectorEnvVarCollection.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// xUnit collection grouping all tests that instantiate <see cref="CollectorWebApplicationFactory"/>
/// (or subclasses) so they execute serially.
/// </summary>
/// <remarks>
/// <para>
/// The factory mutates process-wide environment variables (<c>ASPNETCORE_ENVIRONMENT</c>,
/// <c>AZURE_KEYVAULT_URI</c>, etc.) to bootstrap the host into a clean test state. Even with
/// capture-and-restore in <see cref="CollectorWebApplicationFactory.Dispose(bool)"/>, two factories
/// instantiating concurrently across test classes can interleave their snapshots — caller A
/// captures the originals, caller B captures A's overrides as its "previous" values, and on
/// dispose the wrong baseline is restored.
/// </para>
/// <para>
/// Membership in this collection causes xUnit to run all participating test classes serially,
/// eliminating the interleaving without disabling parallelism for the rest of the suite.
/// </para>
/// </remarks>
[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class CollectorEnvVarCollection
{
    /// <summary>
    /// The collection name applied via <c>[Collection(CollectorEnvVarCollection.CollectionName)]</c>
    /// on each participating test class.
    /// </summary>
    public const string CollectionName = "CollectorEnvVar";
}
