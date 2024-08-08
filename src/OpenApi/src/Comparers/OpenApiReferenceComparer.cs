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
        if (object.ReferenceEquals(x, y))
        {
            return true;
        }

        // We avoid comparing the HostDocument that a reference is associated with
        // so that the same schema in the OpenApiSchemaStore cache and embedded in
        // an OpenAPI document is considered equal.
        return x.ExternalResource == y.ExternalResource &&
            x.Id == y.Id &&
            x.Type == y.Type;
    }

    public int GetHashCode(OpenApiReference obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.ExternalResource);
        hashCode.Add(obj.Id);
        if (obj.Type is not null)
        {
            hashCode.Add(obj.Type);
        }
        return hashCode.ToHashCode();
    }
}
