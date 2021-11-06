// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// Represents an entry in a <see cref="PolicyJumpTable"/>.
/// </summary>
public readonly struct PolicyJumpTableEdge
{
    /// <summary>
    /// Constructs a new <see cref="PolicyJumpTableEdge"/> instance.
    /// </summary>
    /// <param name="state">Represents the match heuristic of the policy.</param>
    /// <param name="destination"></param>
    public PolicyJumpTableEdge(object state, int destination)
    {
        State = state ?? throw new System.ArgumentNullException(nameof(state));
        Destination = destination;
    }

    /// <summary>
    /// Gets the object used to represent the match heuristic. Can be a host, HTTP method, etc.
    /// depending on the matcher policy.
    /// </summary>
    public object State { get; }

    /// <summary>
    /// Gets the destination of the current entry.
    /// </summary>
    public int Destination { get; }
}
