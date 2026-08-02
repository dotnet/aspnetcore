// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Identifies an action that supports the HTTP POST method.
/// </summary>
public class HttpPostAttribute : HttpMethodAttribute
{
    private static readonly IEnumerable<string> _supportedMethods = new[] { "POST" };

    /// <summary>
    /// Creates a new <see cref="HttpPostAttribute"/>.
    /// </summary>
    public HttpPostAttribute()
        : base(_supportedMethods)
    {
    }

    /// <summary>
    /// Creates a new <see cref="HttpPostAttribute"/> with the given route template.
    /// </summary>
    /// <param name="template">The route template. May not be null.</param>
    public HttpPostAttribute([StringSyntax("Route")] string template)
        : base(_supportedMethods, template)
    {
        ArgumentNullException.ThrowIfNull(template);
    }
}
