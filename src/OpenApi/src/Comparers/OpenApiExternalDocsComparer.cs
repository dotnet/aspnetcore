// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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

        return x.Description == y.Description &&
            x.Url == y.Url &&
            x.Extensions.Count == y.Extensions.Count
            && x.Extensions.Keys.All(k => y.Extensions.ContainsKey(k) && y.Extensions[k] == x.Extensions[k]);
    }

    public int GetHashCode(OpenApiExternalDocs obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Description);
        hashCode.Add(obj.Url);
        hashCode.Add(obj.Extensions.Count);
        return hashCode.ToHashCode();
    }
}
