// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

internal class AddOpenApiInvocationComparer : IEqualityComparer<AddOpenApiInvocation>
{
    public static AddOpenApiInvocationComparer Instance { get; } = new();
    public bool Equals(AddOpenApiInvocation? x, AddOpenApiInvocation? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }

        return x.Variant.Equals(y.Variant);
    }

    public int GetHashCode(AddOpenApiInvocation obj) =>
        HashCode.Combine(obj.Variant);
}
