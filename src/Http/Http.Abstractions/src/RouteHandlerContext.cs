// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the information accessible via the route handler filter
/// API when the user is constructing a new route handler.
/// </summary>
public sealed class RouteHandlerContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="RouteHandlerContext"/>.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo"/> associated with the route handler of the current request.</param>
    /// <param name="endpointMetadata">The <see cref="EndpointMetadataCollection"/> associated with the endpoint the filter is targeting.</param>
    public RouteHandlerContext(MethodInfo methodInfo, EndpointMetadataCollection endpointMetadata)
    {
        MethodInfo = methodInfo;
        EndpointMetadata = endpointMetadata;
    }

    /// <summary>
    /// The <see cref="MethodInfo"/> associated with the current route handler.
    /// </summary>
    public MethodInfo MethodInfo { get; }

    /// <summary>
    /// The <see cref="EndpointMetadataCollection"/> associated with the current endpoint.
    /// </summary>
    public EndpointMetadataCollection EndpointMetadata { get; }
}
