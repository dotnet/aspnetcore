// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    public readonly struct PolicyJumpTableEdge
    {
        public PolicyJumpTableEdge(object state, int destination)
        {
            State = state ?? throw new System.ArgumentNullException(nameof(state));
            Destination = destination;
        }

        public object State { get; }

        public int Destination { get; }
    }
}
