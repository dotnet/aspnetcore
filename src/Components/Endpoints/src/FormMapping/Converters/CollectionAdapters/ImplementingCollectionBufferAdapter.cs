// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImplementingCollectionBufferAdapter<TCollection, TBuffer, TElement> : ICollectionBufferAdapter<TCollection, TBuffer, TElement>
    where TBuffer : TCollection, ICollection<TElement>, new()
{
    public static TBuffer CreateBuffer() => new();

    public static TBuffer Add(ref TBuffer buffer, TElement element)
    {
        buffer.Add(element);
        return buffer;
    }

    public static TCollection ToResult(TBuffer buffer) => buffer;
}
