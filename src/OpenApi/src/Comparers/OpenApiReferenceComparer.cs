// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiReferenceComparer : IEqualityComparer<OpenApiReference>
{
    public static OpenApiReferenceComparer Instance { get; } = new OpenApiReferenceComparer();

    public bool Equals(OpenApiReference? x, OpenApiReference? y)
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

    public int GetHashCode(OpenApiReference obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.ExternalResource);
        if (obj.HostDocument is not null)
        {
            // Microsoft.OpenApi provides a HashCode property for
            // the OpenAPI document that we can use to uniquely identify
            // the host document that is referenced here.
            hashCode.Add(obj.HostDocument.HashCode);
        };
        hashCode.Add(obj.Id);
        if (obj.Type is not null)
        {
            hashCode.Add(obj.Type);
        }
        return hashCode.ToHashCode();
    }
}
