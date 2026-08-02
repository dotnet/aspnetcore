// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal static class NormalizedRouteValue
{
    /// <summary>
    /// Gets the case-normalized route value for the specified route <paramref name="key"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="key">The route key to lookup.</param>
    /// <returns>The value corresponding to the key.</returns>
    /// <remarks>
    /// The casing of a route value in <see cref="ActionContext.RouteData"/> is determined by the client.
    /// This making constructing paths for view locations in a case sensitive file system unreliable. Using the
    /// <see cref="Abstractions.ActionDescriptor.RouteValues"/> to get route values
    /// produces consistently cased results.
    /// </remarks>
    public static string? GetNormalizedRouteValue(ActionContext context, string key)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);

        if (!context.RouteData.Values.TryGetValue(key, out var routeValue))
        {
            return null;
        }

        var actionDescriptor = context.ActionDescriptor;
        string? normalizedValue = null;

        if (actionDescriptor.RouteValues.TryGetValue(key, out var value) &&
            !string.IsNullOrEmpty(value))
        {
            normalizedValue = value;
        }

        var stringRouteValue = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
        if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedValue;
        }

        return stringRouteValue;
    }
}
