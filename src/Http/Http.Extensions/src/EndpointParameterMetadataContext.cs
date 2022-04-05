// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the information accessible during endpoint creation by types that implement <see cref="IEndpointParameterMetadataProvider"/>.
/// </summary>
public class EndpointParameterMetadataContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="EndpointParameterMetadataContext"/>.
    /// </summary>
    /// <param name="parameter">The parameter of the route handler delegate of the endpoint being created.</param>
    /// <param name="services">The <see cref="IServiceProvider"/> instance used to access application services.</param>
    /// <param name="endpointMetadata">The objects that will be added to the metadata of the endpoint.</param>
    public EndpointParameterMetadataContext(ParameterInfo parameter, IServiceProvider? services, IList<object> endpointMetadata)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        ArgumentNullException.ThrowIfNull(endpointMetadata, nameof(endpointMetadata));

        Parameter = parameter;
        Services = services;
        EndpointMetadata = endpointMetadata;
    }

    /// <summary>
    /// Gets the parameter of the route handler delegate of the endpoint being created.
    /// </summary>
    public ParameterInfo Parameter { get; internal set; } // internal set to allow re-use

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> associated with the current route handler.
    /// </summary>
    public IServiceProvider? Services { get; }

    /// <summary>
    /// Gets the objects that will be added to the metadata of the endpoint.
    /// </summary>
    public IList<object> EndpointMetadata { get; }
}
