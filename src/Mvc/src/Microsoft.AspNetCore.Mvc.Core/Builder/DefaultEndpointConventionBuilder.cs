// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    internal class DefaultEndpointConventionBuilder : IEndpointConventionBuilder
    {
        public DefaultEndpointConventionBuilder()
        {
            Conventions = new List<Action<EndpointBuilder>>();
        }

        public List<Action<EndpointBuilder>> Conventions { get; }

        public void Add(Action<EndpointBuilder> convention)
        {
            Conventions.Add(convention);
        }
    }
}