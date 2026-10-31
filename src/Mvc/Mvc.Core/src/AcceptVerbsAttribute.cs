// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies what HTTP methods an action supports.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AcceptVerbsAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
    private readonly List<string> _httpMethods;

    private int? _order;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
    /// </summary>
    /// <param name="method">The HTTP method the action supports.</param>
    public AcceptVerbsAttribute(string method)
        : this(new[] { method })
    {
        ArgumentNullException.ThrowIfNull(method);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
    /// </summary>
    /// <param name="methods">The HTTP methods the action supports.</param>
    public AcceptVerbsAttribute(params string[] methods)
    {
        _httpMethods = methods.Select(method => method.ToUpperInvariant()).ToList();
    }

    /// <summary>
    /// Gets the HTTP methods the action supports.
    /// </summary>
    public IEnumerable<string> HttpMethods => _httpMethods;

    /// <summary>
    /// The route template. May be null.
    /// </summary>
    [StringSyntax("Route")]
    public string? Route { get; set; }

    /// <inheritdoc />
    string? IRouteTemplateProvider.Template => Route;

    /// <summary>
    /// Gets the route order. The order determines the order of route execution. Routes with a lower
    /// order value are tried first. When a route doesn't specify a value, it gets the value of the
    /// <see cref="RouteAttribute.Order"/> or a default value of 0 if the <see cref="RouteAttribute"/>
    /// doesn't define a value on the controller.
    /// </summary>
    public int Order
    {
        get { return _order ?? 0; }
        set { _order = value; }
    }

    /// <inheritdoc />
    int? IRouteTemplateProvider.Order => _order;

    /// <inheritdoc />
    public string? Name { get; set; }
}
