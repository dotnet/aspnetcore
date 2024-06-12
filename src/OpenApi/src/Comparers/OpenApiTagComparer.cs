// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// This comparer is used to maintain a globally unique list of tags encountered
/// in a particular OpenAPI document.
/// </summary>
internal sealed class OpenApiTagComparer : IEqualityComparer<OpenApiTag>
{
    public static OpenApiTagComparer Instance { get; } = new OpenApiTagComparer();

    public bool Equals(OpenApiTag? x, OpenApiTag? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        if (object.ReferenceEquals(x, y))
        {
            return true;
        }

        // Tag comparisons are case-sensitive by default. Although the OpenAPI specification
        // only outlines case sensitivity for property names, we extend this principle to
        // property values for tag names as well.
        // See https://spec.openapis.org/oas/v3.1.0#format.
        return string.Equals(x.Name, y.Name, StringComparison.Ordinal);
    }

    public int GetHashCode(OpenApiTag obj) => obj.Name.GetHashCode();
}
