// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template;

/// <summary>
/// Provides methods for parsing route template strings.
/// </summary>
public static class TemplateParser
{
    /// <summary>
    /// Creates a <see cref="RouteTemplate"/> for a given <paramref name="routeTemplate"/> string.
    /// </summary>
    /// <param name="routeTemplate">A string representation of the route template.</param>
    /// <returns>A <see cref="RouteTemplate"/> instance.</returns>
    public static RouteTemplate Parse(string routeTemplate)
    {
        ArgumentNullException.ThrowIfNull(routeTemplate);

        try
        {
            var inner = RoutePatternFactory.Parse(routeTemplate);
            return new RouteTemplate(inner);
        }
        catch (RoutePatternException ex)
        {
            // Preserving the existing behavior of this API even though the logic moved.
            throw new ArgumentException(ex.Message, nameof(routeTemplate), ex);
        }
    }
}
