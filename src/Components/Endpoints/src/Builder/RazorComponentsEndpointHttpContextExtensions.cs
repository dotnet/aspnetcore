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
    /// If the current endpoint is a Razor component, returns the component type.
    /// Otherwise, returns null.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The page component type if the current endpoint is a Razor component; otherwise null.</returns>
    public static Type? GetPageComponentType(this HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type;
}
