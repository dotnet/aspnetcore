// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ConcurrentQueueBufferAdapter<TElement> : ICollectionBufferAdapter<ConcurrentQueue<TElement>, ConcurrentQueue<TElement>, TElement>
{
    public static ConcurrentQueue<TElement> CreateBuffer() => new();

    public static ConcurrentQueue<TElement> Add(ref ConcurrentQueue<TElement> buffer, TElement element)
    {
        buffer.Enqueue(element);
        return buffer;
    }

    public static ConcurrentQueue<TElement> ToResult(ConcurrentQueue<TElement> buffer) => buffer;
}
