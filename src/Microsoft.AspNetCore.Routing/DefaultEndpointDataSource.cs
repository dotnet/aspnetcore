// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    public sealed class DefaultEndpointDataSource : EndpointDataSource
    {
        private readonly List<Endpoint> _endpoints;

        public DefaultEndpointDataSource(params Endpoint[] endpoints)
            : this((IEnumerable<Endpoint>) endpoints)
        {
        }

        public DefaultEndpointDataSource(IEnumerable<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            _endpoints = new List<Endpoint>();
            _endpoints.AddRange(endpoints);
        }

        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

        public override IReadOnlyList<Endpoint> Endpoints => _endpoints;
    }
}
