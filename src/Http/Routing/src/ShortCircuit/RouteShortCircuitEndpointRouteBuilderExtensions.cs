// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// 
/// </summary>
public static class RouteShortCircuitEndpointRouteBuilderExtensions
{
    private static readonly RequestDelegate _shortCircuitDelegate = (context) => Task.CompletedTask;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="statusCode"></param>
    /// <param name="routePrefixes"></param>
    /// <returns></returns>
    public static IEndpointConventionBuilder MapShortCircuit(this IEndpointRouteBuilder builder, int statusCode, params string[] routePrefixes)
    {
        var group = builder.MapGroup("");
        foreach (var routePrefix in routePrefixes)
        {
            string route;
            if (routePrefix.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                route = $"{routePrefix}{{**catchall}}";
            }
            else
            {
                route = $"{routePrefix}/{{**catchall}}";
            }
            group.Map(routePrefix, _shortCircuitDelegate);
        }

        return group.ShortCircuit(statusCode);
    }
}

