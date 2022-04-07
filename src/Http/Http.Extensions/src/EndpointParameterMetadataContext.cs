// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Represents the information accessible during endpoint creation by types that implement <see cref="IEndpointParameterMetadataProvider"/>.
/// </summary>
public class EndpointParameterMetadataContext
{
    /// <summary>
    /// Gets the parameter of the route handler delegate of the endpoint being created.
    /// </summary>
    public ParameterInfo? Parameter { get; init; }

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> associated with the current route handler.
    /// </summary>
    public IServiceProvider? Services { get; init; }

    /// <summary>
    /// Gets the objects that will be added to the metadata of the endpoint.
    /// </summary>
    public IList<object>? EndpointMetadata { get; init; }
}
