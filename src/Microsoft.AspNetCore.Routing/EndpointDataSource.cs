// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class EndpointDataSource
    {
        public abstract IChangeToken GetChangeToken();

        public abstract IReadOnlyList<Endpoint> Endpoints { get; }
    }
}
