// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
