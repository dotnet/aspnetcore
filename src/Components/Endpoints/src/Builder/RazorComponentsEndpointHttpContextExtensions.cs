// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;

[assembly: MetadataUpdateHandler(typeof(RazorComponentsEndpointHttpContextExtensions.MetadataUpdateHandler))]

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Extensions to <see cref="HttpContext"/> for Razor component applications.
/// </summary>
public static class RazorComponentsEndpointHttpContextExtensions
{
    private static readonly ConcurrentDictionary<Type, bool> AcceptsInteractiveRoutingCache = new();

    /// <summary>
    /// Determines whether the current endpoint is a Razor component that can be reached through
    /// interactive routing. This is true for all page components except if they declare the
    /// attribute <see cref="ExcludeFromInteractiveRoutingAttribute"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>True if the current endpoint is a Razor component that does not declare <see cref="ExcludeFromInteractiveRoutingAttribute"/>.</returns>
    public static bool AcceptsInteractiveRouting(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pageType = context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type;

        return pageType is not null
            && AcceptsInteractiveRoutingCache.GetOrAdd(
                pageType,
                static pageType => !pageType.IsDefined(typeof(ExcludeFromInteractiveRoutingAttribute)));
    }

    internal static class MetadataUpdateHandler
    {
        /// <summary>
        /// Invoked as part of <see cref="MetadataUpdateHandlerAttribute" /> contract for hot reload.
        /// </summary>
        public static void ClearCache(Type[]? _)
            => AcceptsInteractiveRoutingCache.Clear();
    }
}
