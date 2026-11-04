// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Maps the trusted distinct-origin MCP app proxy.</summary>
public static class McpAppEndpointRouteBuilderExtensions
{
    /// <summary>Maps a proxy page that accepts messages only from the configured host origin.</summary>
    /// <param name="endpoints">The proxy application's endpoint builder.</param>
    /// <param name="pattern">The proxy endpoint route.</param>
    /// <param name="hostOrigin">The browser-visible origin of the host application.</param>
    /// <param name="options">The proxy's transport bounds, matching the host configuration.</param>
    /// <returns>The endpoint convention builder.</returns>
    public static IEndpointConventionBuilder MapMcpAppProxy(this IEndpointRouteBuilder endpoints, string pattern, Uri hostOrigin,
        McpContentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(hostOrigin);
        return endpoints.MapGet(pattern, async context =>
        {
            var proxyOrigin = new Uri($"{context.Request.Scheme}://{context.Request.Host}");
            var html = McpAppProxy.CreateDocument(hostOrigin, proxyOrigin, options);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsync(html, context.RequestAborted);
        });
    }
}
