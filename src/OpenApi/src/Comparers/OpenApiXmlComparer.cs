// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiXmlComparer : IEqualityComparer<OpenApiXml>
{
    public static OpenApiXmlComparer Instance { get; } = new OpenApiXmlComparer();

    public bool Equals(OpenApiXml? x, OpenApiXml? y)
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

        return x.Name == y.Name &&
            x.Namespace == y.Namespace &&
            x.Prefix == y.Prefix &&
            x.Attribute == y.Attribute &&
            x.Wrapped == y.Wrapped &&
            ComparerHelpers.DictionaryEquals(x.Extensions, y.Extensions, OpenApiAnyComparer.Instance);
    }

    public int GetHashCode(OpenApiXml obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Namespace);
        hashCode.Add(obj.Prefix);
        hashCode.Add(obj.Attribute);
        hashCode.Add(obj.Wrapped);
        hashCode.Add(obj.Extensions.Count);
        return hashCode.ToHashCode();
    }
}
