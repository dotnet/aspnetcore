// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultEndpointConventionBuilder : IEndpointConventionBuilder
    {
        internal EndpointBuilder EndpointBuilder { get; }

        private readonly List<Action<EndpointBuilder>> _conventions;

        public DefaultEndpointConventionBuilder(EndpointBuilder endpointBuilder)
        {
            EndpointBuilder = endpointBuilder;
            _conventions = new List<Action<EndpointBuilder>>();
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            _conventions.Add(convention);
        }

        public Endpoint Build()
        {
            foreach (var convention in _conventions)
            {
                convention(EndpointBuilder);
            }

            return EndpointBuilder.Build();
        }
    }
}
