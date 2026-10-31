// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Builds conventions that will be used for customization of Hub <see cref="EndpointBuilder"/> instances.
/// </summary>
public sealed class HubEndpointConventionBuilder : IHubEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _endpointConventionBuilder;

    internal HubEndpointConventionBuilder(IEndpointConventionBuilder endpointConventionBuilder)
    {
        _endpointConventionBuilder = endpointConventionBuilder;
    }

    /// <summary>
    /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
    /// </summary>
    /// <param name="convention">The convention to add to the builder.</param>
    public void Add(Action<EndpointBuilder> convention)
    {
        _endpointConventionBuilder.Add(convention);
    }

    /// <inheritdoc/>
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        _endpointConventionBuilder.Finally(finalConvention);
    }
}
