// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Identifies an action that supports the HTTP PUT method.
/// </summary>
public class HttpPutAttribute : HttpMethodAttribute
{
    private static readonly IEnumerable<string> s_supportedMethods = new[] { "PUT" };

    /// <summary>
    /// Creates a new <see cref="HttpPutAttribute"/>.
    /// </summary>
    public HttpPutAttribute()
        : base(s_supportedMethods)
    {
    }

    /// <summary>
    /// Creates a new <see cref="HttpPutAttribute"/> with the given route template.
    /// </summary>
    /// <param name="template">The route template. May not be null.</param>
    public HttpPutAttribute([StringSyntax("Route")] string template)
        : base(s_supportedMethods, template)
    {
        ArgumentNullException.ThrowIfNull(template);
    }
}
