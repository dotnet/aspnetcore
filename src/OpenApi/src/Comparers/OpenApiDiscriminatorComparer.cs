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

        return x.PropertyName == y.PropertyName &&
            x.Mapping.Count == y.Mapping.Count &&
            ComparerHelpers.DictionaryEquals(x.Mapping, y.Mapping, StringComparer.Ordinal);
    }

    public int GetHashCode(OpenApiDiscriminator obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.PropertyName);
        hashCode.Add(obj.Mapping.Count);
        return hashCode.ToHashCode();
    }
}
