// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class QueueBufferAdapter<TElement> : ICollectionBufferAdapter<Queue<TElement>, Queue<TElement>, TElement>
{
    public static Queue<TElement> CreateBuffer() => new();

    public static Queue<TElement> Add(ref Queue<TElement> buffer, TElement element)
    {
        buffer.Enqueue(element);
        return buffer;
    }

    public static Queue<TElement> ToResult(Queue<TElement> buffer) => buffer;
}
