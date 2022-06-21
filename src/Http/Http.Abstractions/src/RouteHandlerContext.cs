// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;

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
    /// <param name="endpointMetadata">The <see cref="EndpointBuilder.Metadata"/> associated with the endpoint the filter is targeting.</param>
    /// <param name="applicationServices">The <see cref="IServiceProvider"/> instance used to access the application services.</param>
    public RouteHandlerContext(MethodInfo methodInfo, IList<object> endpointMetadata, IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(endpointMetadata);
        ArgumentNullException.ThrowIfNull(applicationServices);

        MethodInfo = methodInfo;
        EndpointMetadata = endpointMetadata;
        ApplicationServices = applicationServices;
    }

    /// <summary>
    /// The <see cref="MethodInfo"/> associated with the current route handler.
    /// </summary>
    public MethodInfo MethodInfo { get; }

    /// <summary>
    /// The <see cref="EndpointMetadataCollection"/> associated with the current endpoint.
    /// </summary>
    public IList<object> EndpointMetadata { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider ApplicationServices { get; }
}
