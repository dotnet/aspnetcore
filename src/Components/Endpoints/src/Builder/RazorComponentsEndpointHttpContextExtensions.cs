// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Extensions to <see cref="HttpContext"/> for Razor component applications.
/// </summary>
public static class RazorComponentsEndpointHttpContextExtensions
{
    /// <summary>
    /// If the current endpoint is a Razor component, returns the component type.
    /// Otherwise, returns <see langword="null" />.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The page component type if the current endpoint is a Razor component; otherwise <see langword="null" />.</returns>
    public static Type? GetPageComponentType(this HttpContext? context)
        => context?.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type;

    /// <summary>
    /// Determines whether the current endpoint is a Razor component that can be reached through
    /// interactive routing. This is true for all page components except if they declare the
    /// attribute <see cref="AllowInteractiveRoutingAttribute"/> with value <see langword="false"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>True if the current endpoint is a Razor component that does not declare <see cref="AllowInteractiveRoutingAttribute"/> with value <see langword="false"/>.</returns>
    public static bool AllowInteractiveRouting(this HttpContext? context)
    {
        return GetPageComponentType(context) is { } type
            && type.GetCustomAttribute<AllowInteractiveRoutingAttribute>() is not { Allow: false };
    }
}
