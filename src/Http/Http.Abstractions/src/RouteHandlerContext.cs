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
    /// <param name="inferredMetadata">The mutable <see cref="Endpoint"/> metadata inferred about current endpoint. This will come before <see cref="EndpointMetadata"/> in <see cref="Endpoint.Metadata"/>.</param>
    /// <param name="applicationServices">The <see cref="IServiceProvider"/> instance used to access the application services.</param>
    public RouteHandlerContext(MethodInfo methodInfo, IReadOnlyList<object> endpointMetadata, IList<object> inferredMetadata, IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(endpointMetadata);
        ArgumentNullException.ThrowIfNull(inferredMetadata);
        ArgumentNullException.ThrowIfNull(applicationServices);

        MethodInfo = methodInfo;
        EndpointMetadata = endpointMetadata;
        InferredMetadata = inferredMetadata;
        ApplicationServices = applicationServices;
    }

    /// <summary>
    /// The <see cref="MethodInfo"/> associated with the current route handler.
    /// </summary>
    public MethodInfo MethodInfo { get; }

    /// <summary>
    /// The read-only metadata already applied to the current endpoint.
    /// </summary>
    public IReadOnlyList<object> EndpointMetadata { get; }

    /// <summary>
    /// The mutable <see cref="Endpoint"/> metadata inferred about current endpoint. This will come before <see cref="EndpointMetadata"/> in <see cref="Endpoint.Metadata"/>.
    /// </summary>
    public IList<object> InferredMetadata { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider ApplicationServices { get; }
}
