// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="IRouteBuilder"/> to add routes.
/// </summary>
public static class MapRouteRouteBuilderExtensions
{
    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the specified name and template.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    [RequiresUnreferencedCode("This API may perform reflection on supplied parameter which may be trimmed if not referenced directly.")]
    public static IRouteBuilder MapRoute(
        this IRouteBuilder routeBuilder,
        string? name,
        [StringSyntax("Route")] string? template)
    {
        MapRoute(routeBuilder, name, template, defaults: null);
        return routeBuilder;
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the specified name, template, and default values.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <param name="defaults">
    /// An object that contains default values for route parameters. The object's properties represent the names
    /// and values of the default values.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    [RequiresUnreferencedCode("This API may perform reflection on supplied parameter which may be trimmed if not referenced directly.")]
    public static IRouteBuilder MapRoute(
        this IRouteBuilder routeBuilder,
        string? name,
        [StringSyntax("Route")] string? template,
        object? defaults)
    {
        return MapRoute(routeBuilder, name, template, defaults, constraints: null);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the specified name, template, default values, and
    /// constraints.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <param name="defaults">
    /// An object that contains default values for route parameters. The object's properties represent the names
    /// and values of the default values.
    /// </param>
    /// <param name="constraints">
    /// An object that contains constraints for the route. The object's properties represent the names and values
    /// of the constraints.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    [RequiresUnreferencedCode("This API may perform reflection on supplied parameter which may be trimmed if not referenced directly.")]
    public static IRouteBuilder MapRoute(
        this IRouteBuilder routeBuilder,
        string? name,
        [StringSyntax("Route")] string? template,
        object? defaults,
        object? constraints)
    {
        return MapRoute(routeBuilder, name, template, defaults, constraints, dataTokens: null);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> with the specified name, template, default values, and
    /// data tokens.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IRouteBuilder"/> to add the route to.</param>
    /// <param name="name">The name of the route.</param>
    /// <param name="template">The URL pattern of the route.</param>
    /// <param name="defaults">
    /// An object that contains default values for route parameters. The object's properties represent the names
    /// and values of the default values.
    /// </param>
    /// <param name="constraints">
    /// An object that contains constraints for the route. The object's properties represent the names and values
    /// of the constraints.
    /// </param>
    /// <param name="dataTokens">
    /// An object that contains data tokens for the route. The object's properties represent the names and values
    /// of the data tokens.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    [RequiresUnreferencedCode("This API may perform reflection on supplied parameter which may be trimmed if not referenced directly.")]
    public static IRouteBuilder MapRoute(
        this IRouteBuilder routeBuilder,
        string? name,
        [StringSyntax("Route")] string? template,
        object? defaults,
        object? constraints,
        object? dataTokens)
    {
        if (routeBuilder.DefaultHandler == null)
        {
            throw new RouteCreationException(Resources.FormatDefaultHandler_MustBeSet(nameof(IRouteBuilder)));
        }

        routeBuilder.Routes.Add(new Route(
            routeBuilder.DefaultHandler,
            name,
            template,
            new RouteValueDictionary(defaults),
            new RouteValueDictionary(constraints)!,
            new RouteValueDictionary(dataTokens),
            CreateInlineConstraintResolver(routeBuilder.ServiceProvider)));

        return routeBuilder;
    }

    private static IInlineConstraintResolver CreateInlineConstraintResolver(IServiceProvider serviceProvider)
    {
        var inlineConstraintResolver = serviceProvider
            .GetRequiredService<IInlineConstraintResolver>();

        var parameterPolicyFactory = serviceProvider
            .GetRequiredService<ParameterPolicyFactory>();

        // This inline constraint resolver will return a null constraint for non-IRouteConstraint
        // parameter policies so Route does not error
        return new BackCompatInlineConstraintResolver(inlineConstraintResolver, parameterPolicyFactory);
    }

    private sealed class BackCompatInlineConstraintResolver : IInlineConstraintResolver
    {
        private readonly IInlineConstraintResolver _inner;
        private readonly ParameterPolicyFactory _parameterPolicyFactory;

        public BackCompatInlineConstraintResolver(IInlineConstraintResolver inner, ParameterPolicyFactory parameterPolicyFactory)
        {
            _inner = inner;
            _parameterPolicyFactory = parameterPolicyFactory;
        }

        public IRouteConstraint? ResolveConstraint(string inlineConstraint)
        {
            var routeConstraint = _inner.ResolveConstraint(inlineConstraint);
            if (routeConstraint != null)
            {
                return routeConstraint;
            }

            var parameterPolicy = _parameterPolicyFactory.Create(null!, inlineConstraint);
            if (parameterPolicy != null)
            {
                // Logic inside Route will skip adding NullRouteConstraint
                return NullRouteConstraint.Instance;
            }

            return null;
        }
    }
}
