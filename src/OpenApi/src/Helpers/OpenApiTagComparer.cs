
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal class OpenApiTagComparer : IEqualityComparer<OpenApiTag>
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
        return x.Name == y.Name;
    }

    public int GetHashCode(OpenApiTag obj) => obj.Name.GetHashCode();
}
