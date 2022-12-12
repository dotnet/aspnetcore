// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

internal class TestEndpointBuilder : EndpointBuilder
{
    public override Endpoint Build()
    {
        return new Endpoint(RequestDelegate, new EndpointMetadataCollection(Metadata), DisplayName);
    }
}

internal class TestEndpointConventionBuilder : IEndpointConventionBuilder
{
    public IList<Action<EndpointBuilder>> Conventions { get; } = new List<Action<EndpointBuilder>>();

    public void Add(Action<EndpointBuilder> convention)
    {
        Conventions.Add(convention);
    }

    public TestEndpointConventionBuilder ApplyToEndpoint(EndpointBuilder endpoint)
    {
        foreach (var convention in Conventions)
        {
            convention(endpoint);
        }

        return this;
    }
}
