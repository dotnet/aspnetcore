// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Supports implementing a handler that executes for a given route.
/// </summary>
public class RouteHandler : IRouteHandler, IRouter
{
    private readonly RequestDelegate _requestDelegate;

    /// <summary>
    /// Constructs a new <see cref="RouteHandler"/> instance.
    /// </summary>
    /// <param name="requestDelegate">The delegate used to process requests.</param>
    public RouteHandler(RequestDelegate requestDelegate)
    {
        _requestDelegate = requestDelegate;
    }

    /// <inheritdoc />
    public RequestDelegate GetRequestHandler(HttpContext httpContext, RouteData routeData)
    {
        return _requestDelegate;
    }

    /// <inheritdoc />
    public VirtualPathData? GetVirtualPath(VirtualPathContext context)
    {
        // Nothing to do.
        return null;
    }

    /// <inheritdoc />
    public Task RouteAsync(RouteContext context)
    {
        context.Handler = _requestDelegate;
        return Task.CompletedTask;
    }
}
