// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// The implementation of <see cref="IActionConstraint" /> used to enforce
/// HTTP method filtering when MVC is used with legacy <see cref="IRouter" />
/// support. The <see cref="HttpMethodActionConstraint" /> can be used to determine
/// the set of HTTP methods supported by an action.
/// </summary>
public class HttpMethodActionConstraint : IActionConstraint
{
    /// <summary>
    /// The <see cref="IActionConstraint.Order" /> value used by <see cref="HttpMethodActionConstraint" />.
    /// </summary>
    public static readonly int HttpMethodConstraintOrder = 100;

    private readonly IReadOnlyList<string> _httpMethods;

    /// <summary>
    /// Creates a new instance of <see cref="HttpMethodActionConstraint" />.
    /// </summary>
    /// <param name="httpMethods">
    /// The list of HTTP methods to allow. Providing an empty list will allow
    /// any HTTP method.
    /// </param>
    public HttpMethodActionConstraint(IEnumerable<string> httpMethods)
    {
        ArgumentNullException.ThrowIfNull(httpMethods);

        var methods = new List<string>();

        foreach (var method in httpMethods)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException($"{nameof(httpMethods)} cannot contain null or empty strings.");
            }

            methods.Add(method);
        }

        _httpMethods = new ReadOnlyCollection<string>(methods);
    }

    /// <summary>
    /// Gets the list of allowed HTTP methods. Will return an empty list if all HTTP methods are allowed.
    /// </summary>
    public IEnumerable<string> HttpMethods => _httpMethods;

    /// <inheritdoc />
    public int Order => HttpMethodConstraintOrder;

    /// <inheritdoc />
    public virtual bool Accept(ActionConstraintContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_httpMethods.Count == 0)
        {
            return true;
        }

        var request = context.RouteContext.HttpContext.Request;
        var method = request.Method;

        for (var i = 0; i < _httpMethods.Count; i++)
        {
            var supportedMethod = _httpMethods[i];
            if (string.Equals(supportedMethod, method, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
