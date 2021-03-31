// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    internal class ModelEndpointDataSource : EndpointDataSource
    {
        private List<DefaultEndpointConventionBuilder> _endpointConventionBuilders;

        public ModelEndpointDataSource()
        {
            _endpointConventionBuilders = new List<DefaultEndpointConventionBuilder>();
        }

        public IEndpointConventionBuilder AddEndpointBuilder(EndpointBuilder endpointBuilder)
        {
            var builder = new DefaultEndpointConventionBuilder(endpointBuilder);
            _endpointConventionBuilders.Add(builder);

            return builder;
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        public override IReadOnlyList<Endpoint> Endpoints => _endpointConventionBuilders.Select(e => e.Build()).ToArray();

        // for testing
        internal IEnumerable<EndpointBuilder> EndpointBuilders => _endpointConventionBuilders.Select(b => b.EndpointBuilder);
    }
}
