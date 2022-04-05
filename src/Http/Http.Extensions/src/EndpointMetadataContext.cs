// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the information accessible during endpoint creation by types that implement <see cref="IEndpointMetadataProvider"/>.
/// </summary>
public sealed class EndpointMetadataContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="EndpointMetadataContext"/>.
    /// </summary>
    /// <param name="method">The <see cref="MethodInfo"/> associated with the current route handler.</param>
    /// <param name="services">The <see cref="IServiceProvider"/> instance used to access application services.</param>
    /// <param name="endpointMetadata">The objects that will be added to the metadata of the endpoint.</param>
    public EndpointMetadataContext(MethodInfo method, IServiceProvider? services, IList<object> endpointMetadata)
    {
        ArgumentNullException.ThrowIfNull(method, nameof(method));
        ArgumentNullException.ThrowIfNull(endpointMetadata, nameof(endpointMetadata));

        Method = method;
        Services = services;
        EndpointMetadata = endpointMetadata;
    }

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> associated with the current route handler.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider? Services { get; }

    /// <summary>
    /// Gets the objects that will be added to the metadata of the endpoint.
    /// </summary>
    public IList<object> EndpointMetadata { get; }
}
