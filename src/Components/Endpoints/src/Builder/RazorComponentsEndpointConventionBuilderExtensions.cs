// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Configures which assemblies are part of the given Razor Component Application.
/// </summary>
public static class RazorComponentsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds the given additional assemblies to the component application.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentsEndpointConventionBuilder"/>.</param>"/>
    /// <param name="assemblies">The <see cref="Assembly"/> instances to add.</param>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    /// <remarks>
    /// The provided assemblies will be scanned for pages that will be mapped as endpoints.
    /// </remarks>
    public static RazorComponentsEndpointConventionBuilder AddAdditionalAssemblies(
        this RazorComponentsEndpointConventionBuilder builder,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            builder.ApplicationBuilder.AddAssembly(assembly);
        }
        return builder;
    }
}
