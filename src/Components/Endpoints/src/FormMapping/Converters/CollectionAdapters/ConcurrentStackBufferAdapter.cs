// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ConcurrentStackBufferAdapter<TElement> : ICollectionBufferAdapter<ConcurrentStack<TElement>, ConcurrentStack<TElement>, TElement>
{
    public static ConcurrentStack<TElement> CreateBuffer() => new();

    public static ConcurrentStack<TElement> Add(ref ConcurrentStack<TElement> buffer, TElement element)
    {
        buffer.Push(element);
        return buffer;
    }

    public static ConcurrentStack<TElement> ToResult(ConcurrentStack<TElement> buffer) => buffer;
}
