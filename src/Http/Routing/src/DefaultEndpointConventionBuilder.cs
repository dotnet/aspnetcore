// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

internal class DefaultEndpointConventionBuilder : IEndpointConventionBuilder
{
    internal EndpointBuilder EndpointBuilder { get; }

    private List<Action<EndpointBuilder>>? _conventions;

    public DefaultEndpointConventionBuilder(EndpointBuilder endpointBuilder)
    {
        EndpointBuilder = endpointBuilder;
        _conventions = new();
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        var conventions = _conventions;

        if (conventions is null)
        {
            throw new InvalidOperationException("Conventions cannot be added after building the endpoint");
        }

        conventions.Add(convention);
    }

    public Endpoint Build()
    {
        // Only apply the conventions once
        var conventions = Interlocked.Exchange(ref _conventions, null);

        if (conventions is not null)
        {
            foreach (var convention in conventions)
            {
                convention(EndpointBuilder);
            }
        }

        return EndpointBuilder.Build();
    }
}
