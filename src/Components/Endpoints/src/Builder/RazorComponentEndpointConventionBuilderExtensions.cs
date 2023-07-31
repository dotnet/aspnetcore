// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Discovery;

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
    public static RazorComponentEndpointConventionBuilder AddComponentAssemblies(
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

    /// <summary>
    /// Removes the given assemblies from the component application.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentEndpointConventionBuilder"/>.</param>"/>
    /// <param name="assemblies">The <see cref="Assembly"/> instances to remove.</param>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    public static RazorComponentEndpointConventionBuilder RemoveComponentAssemblies(
        this RazorComponentEndpointConventionBuilder builder,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            builder.ApplicationBuilder.RemoveAssembly(assembly);
        }
        return builder;
    }

    /// <summary>
    /// Configures which assemblies are part of the component application.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentEndpointConventionBuilder"/>.</param>"/>
    /// <param name="configure">An <see cref="Action{ComponentApplicationBuilder}" /> to configure
    /// the provided <see cref="ComponentApplicationBuilder"/>.</param>
    public static RazorComponentEndpointConventionBuilder ConfigureComponentAssemblies(
        this RazorComponentEndpointConventionBuilder builder,
        Action<ComponentApplicationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var appBuilder = builder.ApplicationBuilder;
        configure(appBuilder);
        return builder;
    }
}
