// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// Identifies an action that supports a given set of HTTP methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class HttpMethodAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
    private readonly List<string> _httpMethods;

    private int? _order;

    /// <summary>
    /// Creates a new <see cref="HttpMethodAttribute"/> with the given
    /// set of HTTP methods.
    /// <param name="httpMethods">The set of supported HTTP methods. May not be null.</param>
    /// </summary>
    public HttpMethodAttribute(IEnumerable<string> httpMethods)
        : this(httpMethods, null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="HttpMethodAttribute"/> with the given
    /// set of HTTP methods an the given route template.
    /// </summary>
    /// <param name="httpMethods">The set of supported methods. May not be null.</param>
    /// <param name="template">The route template.</param>
    public HttpMethodAttribute(IEnumerable<string> httpMethods, [StringSyntax("Route")] string? template)
    {
        ArgumentNullException.ThrowIfNull(httpMethods);

        _httpMethods = httpMethods.ToList();
        Template = template;
    }

    /// <inheritdoc />
    public IEnumerable<string> HttpMethods => _httpMethods;

    /// <inheritdoc />
    [StringSyntax("Route")]
    public string? Template { get; }

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
    [DisallowNull]
    public string? Name { get; set; }
}
