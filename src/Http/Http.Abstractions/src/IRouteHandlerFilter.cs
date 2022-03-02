// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides an interface for implementing a filter targetting a route handler.
/// </summary>
public interface IRouteHandlerFilter
{
    /// <summary>
    /// Implements the core logic associated with the filter given a <see cref="RouteHandlerFilterContext"/>
    /// and the next filter to call in the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="RouteHandlerFilterContext"/> associated with the current request/response.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>The result of calling the current filter.</returns>
    ValueTask<object?> InvokeAsync(RouteHandlerFilterContext context, Func<RouteHandlerFilterContext, ValueTask<object?>> next);
}
