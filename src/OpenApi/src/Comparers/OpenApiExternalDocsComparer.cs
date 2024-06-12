// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiExternalDocsComparer : IEqualityComparer<OpenApiExternalDocs>
{
    public static OpenApiExternalDocsComparer Instance { get; } = new OpenApiExternalDocsComparer();

    public bool Equals(OpenApiExternalDocs? x, OpenApiExternalDocs? y)
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

    public int GetHashCode(OpenApiExternalDocs obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Description);
        hashCode.Add(obj.Url);
        foreach (var item in obj.Extensions)
        {
            hashCode.Add(item.Key, StringComparer.Ordinal);
            if (item.Value is IOpenApiAny openApiAny)
            {
                hashCode.Add(openApiAny, OpenApiAnyComparer.Instance);
            }
            else
            {
                hashCode.Add(item.Value);
            }
        }
        return hashCode.ToHashCode();
    }
}
