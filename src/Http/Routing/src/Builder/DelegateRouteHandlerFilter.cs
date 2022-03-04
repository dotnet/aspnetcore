// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal sealed class DelegateRouteHandlerFilter : IRouteHandlerFilter
{
    private readonly Func<RouteHandlerFilterContext, Func<RouteHandlerFilterContext, ValueTask<object?>>, ValueTask<object?>> _routeHandlerFilter;

    internal DelegateRouteHandlerFilter(Func<RouteHandlerFilterContext, Func<RouteHandlerFilterContext, ValueTask<object?>>, ValueTask<object?>> routeHandlerFilter)
    {
        _routeHandlerFilter = routeHandlerFilter;
    }

    public ValueTask<object?> InvokeAsync(RouteHandlerFilterContext context, Func<RouteHandlerFilterContext, ValueTask<object?>> next)
    {
        return _routeHandlerFilter(context, next);
    }
}
