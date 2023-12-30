// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ArrayCollectionFactory<TElement> : ICollectionFactory<TElement[], TElement>
{
    public static TElement[] ToResultCore(TElement[] buffer, int size)
    {
        var result = new TElement[size];
        Array.Copy(buffer, result, size);
        return result;
    }
}
