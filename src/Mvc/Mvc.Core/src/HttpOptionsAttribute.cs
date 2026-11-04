// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Identifies an action that supports the HTTP OPTIONS method.
/// </summary>
public class HttpOptionsAttribute : HttpMethodAttribute
{
    private static readonly IEnumerable<string> _supportedMethods = new[] { "OPTIONS" };

    /// <summary>
    /// Creates a new <see cref="HttpOptionsAttribute"/>.
    /// </summary>
    public HttpOptionsAttribute()
        : base(_supportedMethods)
    {
    }

    /// <summary>
    /// Creates a new <see cref="HttpOptionsAttribute"/> with the given route template.
    /// </summary>
    /// <param name="template">The route template. May not be null.</param>
    public HttpOptionsAttribute([StringSyntax("Route")] string template)
        : base(_supportedMethods, template)
    {
        ArgumentNullException.ThrowIfNull(template);
    }
}
