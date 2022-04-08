// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Represents the information accessible during endpoint creation by types that implement <see cref="IEndpointMetadataProvider"/>.
/// </summary>
public sealed class EndpointMetadataContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="EndpointMetadataContext"/> class.
    /// </summary>
    /// <param name="method">The <see cref="MethodInfo"/> of the route handler delegate of the endpoint being created.</param>
    /// <param name="endpointMetadata">The list of objects that will be added to the metadata of the endpoint.</param>
    /// <param name="services">The <see cref="IServiceProvider"/> instance used to access application services.</param>
    public EndpointMetadataContext(MethodInfo method, IList<object> endpointMetadata, IServiceProvider? services)
    {
        Method = method;
        EndpointMetadata = endpointMetadata;
        Services = services;
    }

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> of the route handler delegate of the endpoint being created.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the list of objects that will be added to the metadata of the endpoint.
    /// </summary>
    public IList<object> EndpointMetadata { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider? Services { get; }
}
