// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Configures which assemblies are part of the given Razor Component Application.
/// </summary>
public static class RazorComponentEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds the given assemblies to the component application.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentEndpointConventionBuilder"/>.</param>"/>
    /// <param name="assemblies">The <see cref="Assembly"/> instances to add.</param>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    /// <remarks>
    /// The provided assemblies will be scanned for pages that will be mapped as endpoints.
    /// </remarks>
    public static RazorComponentEndpointConventionBuilder AddAssemblies(
        this RazorComponentEndpointConventionBuilder builder,
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
