// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// An address of route name and values.
/// </summary>
public class RouteValuesAddress
{
    private string? _toString;
    /// <summary>
    /// Gets or sets the route name.
    /// </summary>
    public string? RouteName { get; set; }

    /// <summary>
    /// Gets or sets the route values that are explicitly specified.
    /// </summary>
    public RouteValueDictionary ExplicitValues { get; set; } = default!;

    /// <summary>
    /// Gets or sets ambient route values from the current HTTP request.
    /// </summary>
    public RouteValueDictionary? AmbientValues { get; set; }

    /// <inheritdoc />
    public override string? ToString()
    {
        _toString ??= $"{RouteName}({string.Join(',', ExplicitValues.Select(kv => $"{kv.Key}=[{kv.Value}]"))})";
        return _toString;
    }
}
