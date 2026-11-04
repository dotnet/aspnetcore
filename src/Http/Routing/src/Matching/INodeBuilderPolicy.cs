// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// Implements an interface for a matcher policy with support for generating graph representations of the endpoints.
/// </summary>
public interface INodeBuilderPolicy
{
    /// <summary>
    /// Evaluates if the policy matches any of the endpoints provided in <paramref name="endpoints"/>.
    /// </summary>
    /// <param name="endpoints">A list of <see cref="Endpoint"/>.</param>
    /// <returns><see langword="true"/> if the policy applies to any of the provided <paramref name="endpoints"/>.</returns>
    bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);

    /// <summary>
    /// Generates a graph that representations the relationship between endpoints and hosts.
    /// </summary>
    /// <param name="endpoints">A list of <see cref="Endpoint"/>.</param>
    /// <returns>A graph representing the relationship between endpoints and hosts.</returns>
    IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints);

    /// <summary>
    /// Constructs a jump table given the a set of <paramref name="edges"/>.
    /// </summary>
    /// <param name="exitDestination">The default destination for lookups.</param>
    /// <param name="edges">A list of <see cref="PolicyJumpTableEdge"/>.</param>
    /// <returns>A <see cref="PolicyJumpTable"/> instance.</returns>
    PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges);
}
