// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides an interface for implementing a filter targetting a route handler.
/// </summary>
public interface IRouteHandlerFilter
{
    /// <summary>
    /// Implements the core logic associated with the filter given a <see cref="RouteHandlerInvocationContext"/>
    /// and the next filter to call in the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="RouteHandlerInvocationContext"/> associated with the current request/response.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <param name="routeHandlerContext">The <see cref="RouteHandlerContext"/> associated with the handler the filter is registered on.</param>
    /// <returns>An awaitable result of calling the handler and apply
    /// any modifications made by filters in the pipeline.</returns>
    ValueTask<object?> InvokeAsync(RouteHandlerInvocationContext context, RouteHandlerFilterDelegate next, RouteHandlerContext routeHandlerContext);
}
