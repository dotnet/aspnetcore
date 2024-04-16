// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Extensions to <see cref="HttpContext"/> for Razor component applications.
/// </summary>
public static class RazorComponentsEndpointHttpContextExtensions
{
    /// <summary>
    /// Determines whether the current endpoint is a Razor component that can be reached through
    /// interactive routing. This is true for all page components except if they declare the
    /// attribute <see cref="AllowInteractiveRoutingAttribute"/> with value <see langword="false"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>True if the current endpoint is a Razor component that does not declare <see cref="AllowInteractiveRoutingAttribute"/> with value <see langword="false"/>.</returns>
    public static bool AllowsInteractiveRouting(this HttpContext? context)
    {
        var metadata = context?.GetEndpoint()?.Metadata;
        return metadata?.GetMetadata<ComponentTypeMetadata>() is not null
            && metadata.GetMetadata<AllowInteractiveRoutingAttribute>() is not { Allow: false };
    }
}
