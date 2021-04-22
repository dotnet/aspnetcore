// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// Represents an edge in a matcher policy graph.
    /// </summary>
    public readonly struct PolicyNodeEdge
    {
        /// <summary>
        /// Constructs a new <see cref="PolicyNodeEdge"/> instance.
        /// </summary>
        /// <param name="state">Represents the match heuristic of the policy.</param>
        /// <param name="endpoints">Represents the endpoints that match the policy</param>
        public PolicyNodeEdge(object state, IReadOnlyList<Endpoint> endpoints)
        {
            State = state ?? throw new System.ArgumentNullException(nameof(state));
            Endpoints = endpoints ?? throw new System.ArgumentNullException(nameof(endpoints));
        }

        /// <summary>
        /// Gets the endpoints that match the policy defined by <see cref="State"/>.
        /// </summary>
        public IReadOnlyList<Endpoint> Endpoints { get; }

        /// <summary>
        /// Gets the object used to represent the match heuristic. Can be a host, HTTP method, etc.
        /// depending on the matcher policy.
        /// </summary>
        public object State { get; }
    }
}
