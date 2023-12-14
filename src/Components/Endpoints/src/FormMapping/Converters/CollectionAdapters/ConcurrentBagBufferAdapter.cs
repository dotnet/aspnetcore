// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ConcurrentBagBufferAdapter<TElement> : ICollectionBufferAdapter<ConcurrentBag<TElement>, ConcurrentBag<TElement>, TElement>
{
    public static ConcurrentBag<TElement> CreateBuffer() => new();

    public static ConcurrentBag<TElement> Add(ref ConcurrentBag<TElement> buffer, TElement element)
    {
        buffer.Add(element);
        return buffer;
    }

    public static ConcurrentBag<TElement> ToResult(ConcurrentBag<TElement> buffer) => buffer;
}
