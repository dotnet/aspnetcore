// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// Interface for attributes which can supply a route template for attribute routing.
/// </summary>
public interface IRouteTemplateProvider
{
    /// <summary>
    /// The route template. May be <see langword="null"/>.
    /// </summary>
    [StringSyntax("Route")]
    string? Template { get; }

    /// <summary>
    /// Gets the route order. The order determines the order of route execution. Routes with a lower
    /// order value are tried first. When a route doesn't specify a value, it gets a default value of 0.
    /// A null value for the Order property means that the user didn't specify an explicit order for the
    /// route.
    /// </summary>
    int? Order { get; }

    /// <summary>
    /// Gets the route name. The route name can be used to generate a link using a specific route, instead
    ///  of relying on selection of a route based on the given set of route values.
    /// </summary>
    string? Name { get; }
}
