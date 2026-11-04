// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IRouteBuilder"/>.
/// </summary>
public static class MvcAreaRouteBuilderExtensions
{
    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the given MVC area with the specified
    /// <paramref name="name"/>, <paramref name="areaName"/> and <paramref name="template"/>.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="areaName">The MVC area name.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IRouteBuilder MapAreaRoute(
        this IRouteBuilder routeBuilder,
        string name,
        string areaName,
        [StringSyntax("Route")] string? template)
    {
        MapAreaRoute(routeBuilder, name, areaName, template, defaults: null, constraints: null, dataTokens: null);
        return routeBuilder;
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the given MVC area with the specified
    /// <paramref name="name"/>, <paramref name="areaName"/>, <paramref name="template"/>, and
    /// <paramref name="defaults"/>.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="areaName">The MVC area name.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <param name="defaults">
    /// An object that contains default values for route parameters. The object's properties represent the
    /// names and values of the default values.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IRouteBuilder MapAreaRoute(
        this IRouteBuilder routeBuilder,
        string name,
        string areaName,
        [StringSyntax("Route")] string? template,
        object? defaults)
    {
        MapAreaRoute(routeBuilder, name, areaName, template, defaults, constraints: null, dataTokens: null);
        return routeBuilder;
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the given MVC area with the specified
    /// <paramref name="name"/>, <paramref name="areaName"/>, <paramref name="template"/>,
    /// <paramref name="defaults"/>, and <paramref name="constraints"/>.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="areaName">The MVC area name.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <param name="defaults">
    /// An object that contains default values for route parameters. The object's properties represent the
    /// names and values of the default values.
    /// </param>
    /// <param name="constraints">
    /// An object that contains constraints for the route. The object's properties represent the names and
    /// values of the constraints.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IRouteBuilder MapAreaRoute(
        this IRouteBuilder routeBuilder,
        string name,
        string areaName,
        [StringSyntax("Route")] string? template,
        object? defaults,
        object? constraints)
    {
        MapAreaRoute(routeBuilder, name, areaName, template, defaults, constraints, dataTokens: null);
        return routeBuilder;
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the given MVC area with the specified
    /// <paramref name="name"/>, <paramref name="areaName"/>, <paramref name="template"/>,
    /// <paramref name="defaults"/>, <paramref name="constraints"/>, and <paramref name="dataTokens"/>.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="areaName">The MVC area name.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <param name="defaults">
    /// An object that contains default values for route parameters. The object's properties represent the
    /// names and values of the default values.
    /// </param>
    /// <param name="constraints">
    /// An object that contains constraints for the route. The object's properties represent the names and
    /// values of the constraints.
    /// </param>
    /// <param name="dataTokens">
    /// An object that contains data tokens for the route. The object's properties represent the names and
    /// values of the data tokens.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IRouteBuilder MapAreaRoute(
        this IRouteBuilder routeBuilder,
        string name,
        string areaName,
        [StringSyntax("Route")] string? template,
        object? defaults,
        object? constraints,
        object? dataTokens)
    {
        ArgumentNullException.ThrowIfNull(routeBuilder);
        ArgumentException.ThrowIfNullOrEmpty(areaName);

        var defaultsDictionary = new RouteValueDictionary(defaults);
        defaultsDictionary["area"] = defaultsDictionary["area"] ?? areaName;

        var constraintsDictionary = new RouteValueDictionary(constraints);
        constraintsDictionary["area"] = constraintsDictionary["area"] ?? new StringRouteConstraint(areaName);

        routeBuilder.MapRoute(name, template, defaultsDictionary, constraintsDictionary, dataTokens);
        return routeBuilder;
    }
}
