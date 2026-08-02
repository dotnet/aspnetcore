// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints.Infrastructure;

/// <summary>
/// This type is not recommended for use outside of the Blazor framework.
/// </summary>
public static class ComponentEndpointConventionBuilderHelper
{
    /// <summary>
    /// This method is not recommended for use outside of the Blazor framework.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="renderMode"></param>
    public static void AddRenderMode(RazorComponentsEndpointConventionBuilder builder, IComponentRenderMode renderMode)
    {
        builder.AddRenderMode(renderMode);
    }

    /// <summary>
    /// This method is not recommended for use outside of the Blazor framework.
    /// </summary>
    /// <param name="builder"></param>
    public static IEndpointRouteBuilder GetEndpointRouteBuilder(RazorComponentsEndpointConventionBuilder builder) => builder.EndpointRouteBuilder;
}

