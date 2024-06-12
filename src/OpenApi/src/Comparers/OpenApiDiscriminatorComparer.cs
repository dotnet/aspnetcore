// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiDiscriminatorComparer : IEqualityComparer<OpenApiDiscriminator>
{
    public static OpenApiDiscriminatorComparer Instance { get; } = new OpenApiDiscriminatorComparer();

    public bool Equals(OpenApiDiscriminator? x, OpenApiDiscriminator? y)
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

        return GetHashCode(x) == GetHashCode(y);
    }

    public int GetHashCode(OpenApiDiscriminator obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.PropertyName);
        foreach (var item in obj.Mapping)
        {
            hashCode.Add(item.Key);
            hashCode.Add(item.Value);
        }
        return hashCode.ToHashCode();
    }
}
