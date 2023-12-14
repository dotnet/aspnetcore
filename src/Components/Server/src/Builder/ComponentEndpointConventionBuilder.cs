// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Builds conventions that will be used for customization of ComponentHub <see cref="EndpointBuilder"/> instances.
/// </summary>
public sealed class ComponentEndpointConventionBuilder : IHubEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _hubEndpoint;
    private readonly IEndpointConventionBuilder _disconnectEndpoint;
    private readonly IEndpointConventionBuilder _jsInitializersEndpoint;
    private readonly IEndpointConventionBuilder _blazorEndpoint;

    internal ComponentEndpointConventionBuilder(
        IEndpointConventionBuilder hubEndpoint,
        IEndpointConventionBuilder disconnectEndpoint,
        IEndpointConventionBuilder jsInitializersEndpoint,
        IEndpointConventionBuilder blazorEndpoint)
    {
        _hubEndpoint = hubEndpoint;
        _disconnectEndpoint = disconnectEndpoint;
        _jsInitializersEndpoint = jsInitializersEndpoint;
        _blazorEndpoint = blazorEndpoint;
    }

    /// <summary>
    /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
    /// </summary>
    /// <param name="convention">The convention to add to the builder.</param>
    public void Add(Action<EndpointBuilder> convention)
    {
        _hubEndpoint.Add(convention);
        _disconnectEndpoint.Add(convention);
        _jsInitializersEndpoint.Add(convention);
        _blazorEndpoint.Add(convention);
    }

    /// <inheritdoc/>
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        _hubEndpoint.Finally(finalConvention);
        _disconnectEndpoint.Finally(finalConvention);
        _jsInitializersEndpoint.Finally(finalConvention);
        _blazorEndpoint.Finally(finalConvention);
    }
}
