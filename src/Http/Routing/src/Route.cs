// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Represents an instance of a route.
/// </summary>
public class Route : RouteBase
{
    private readonly IRouter _target;

    /// <summary>
    /// Constructs a new <see cref="Route"/> instance.
    /// </summary>
    /// <param name="target">An <see cref="IRouter"/> instance associated with the component.</param>
    /// <param name="routeTemplate">A string representation of the route template.</param>
    /// <param name="inlineConstraintResolver">An <see cref="IInlineConstraintResolver"/> used for resolving inline constraints.</param>
    public Route(
        IRouter target,
        [StringSyntax("Route")] string routeTemplate,
        IInlineConstraintResolver inlineConstraintResolver)
        : this(
            target,
            routeTemplate,
            defaults: null,
            constraints: null,
            dataTokens: null,
            inlineConstraintResolver: inlineConstraintResolver)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="Route"/> instance.
    /// </summary>
    /// <param name="target">An <see cref="IRouter"/> instance associated with the component.</param>
    /// <param name="routeTemplate">A string representation of the route template.</param>
    /// <param name="defaults">The default values for parameters in the route.</param>
    /// <param name="constraints">The constraints for the route.</param>
    /// <param name="dataTokens">The data tokens for the route.</param>
    /// <param name="inlineConstraintResolver">An <see cref="IInlineConstraintResolver"/> used for resolving inline constraints.</param>
    public Route(
        IRouter target,
        [StringSyntax("Route")] string routeTemplate,
        RouteValueDictionary? defaults,
        IDictionary<string, object>? constraints,
        RouteValueDictionary? dataTokens,
        IInlineConstraintResolver inlineConstraintResolver)
        : this(target, null, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="Route"/> instance.
    /// </summary>
    /// <param name="target">An <see cref="IRouter"/> instance associated with the component.</param>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="routeTemplate">A string representation of the route template.</param>
    /// <param name="defaults">The default values for parameters in the route.</param>
    /// <param name="constraints">The constraints for the route.</param>
    /// <param name="dataTokens">The data tokens for the route.</param>
    /// <param name="inlineConstraintResolver">An <see cref="IInlineConstraintResolver"/> used for resolving inline constraints.</param>
    public Route(
        IRouter target,
        string? routeName,
        [StringSyntax("Route")] string? routeTemplate,
        RouteValueDictionary? defaults,
        IDictionary<string, object>? constraints,
        RouteValueDictionary? dataTokens,
        IInlineConstraintResolver inlineConstraintResolver)
        : base(
              routeTemplate,
              routeName,
              inlineConstraintResolver,
              defaults,
              constraints,
              dataTokens)
    {
        ArgumentNullException.ThrowIfNull(target);

        _target = target;
    }

    /// <summary>
    /// Gets a string representation of the route template.
    /// </summary>
    public string? RouteTemplate => ParsedTemplate.TemplateText;

    /// <inheritdoc />
    protected override Task OnRouteMatched(RouteContext context)
    {
        context.RouteData.Routers.Add(_target);
        return _target.RouteAsync(context);
    }

    /// <inheritdoc />
    protected override VirtualPathData? OnVirtualPathGenerated(VirtualPathContext context)
    {
        return _target.GetVirtualPath(context);
    }
}
