// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal abstract class ArrayPoolBufferAdapter<TCollection, TCollectionFactory, TElement>
    : ICollectionBufferAdapter<TCollection, ArrayPoolBufferAdapter<TCollection, TCollectionFactory, TElement>.PooledBuffer, TElement>
    where TCollectionFactory : ICollectionFactory<TCollection, TElement>
{
    public static PooledBuffer CreateBuffer() => new() { Data = ArrayPool<TElement>.Shared.Rent(16), Count = 0 };

    public static PooledBuffer Add(ref PooledBuffer buffer, TElement element)
    {
        if (buffer.Count >= buffer.Data.Length)
        {
            var newBuffer = ArrayPool<TElement>.Shared.Rent(buffer.Data.Length * 2);
            Array.Copy(buffer.Data, newBuffer, buffer.Data.Length);
            ArrayPool<TElement>.Shared.Return(buffer.Data);
            buffer.Data = newBuffer;
        }

        buffer.Data[buffer.Count++] = element;
        return buffer;
    }

    public static TCollection ToResult(PooledBuffer buffer)
    {
        var result = TCollectionFactory.ToResultCore(buffer.Data, buffer.Count);
        ArrayPool<TElement>.Shared.Return(buffer.Data);
        return result;
    }

    public struct PooledBuffer
    {
        public TElement[] Data { get; set; }
        public int Count { get; set; }
    }
}
