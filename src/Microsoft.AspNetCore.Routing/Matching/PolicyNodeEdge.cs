// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public readonly struct PolicyNodeEdge
    {
        public PolicyNodeEdge(object state, IReadOnlyList<Endpoint> endpoints)
        {
            State = state ?? throw new System.ArgumentNullException(nameof(state));
            Endpoints = endpoints ?? throw new System.ArgumentNullException(nameof(endpoints));
        }

        public IReadOnlyList<Endpoint> Endpoints { get; }

        public object State { get; }
    }
}
