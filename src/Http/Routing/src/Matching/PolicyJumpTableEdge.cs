// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
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
}
