// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Any;
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

        return GetHashCode(x) == GetHashCode(y);
    }

    public int GetHashCode([DisallowNull] OpenApiXml obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Namespace);
        hashCode.Add(obj.Prefix);
        hashCode.Add(obj.Attribute);
        hashCode.Add(obj.Wrapped);
        foreach (var item in obj.Extensions)
        {
            hashCode.Add(item.Key);
            if (item.Value is IOpenApiAny any)
            {
                hashCode.Add(any, OpenApiAnyComparer.Instance);
            }
            {
                hashCode.Add(item.Value);
            }
        }
        return hashCode.ToHashCode();
    }
}
