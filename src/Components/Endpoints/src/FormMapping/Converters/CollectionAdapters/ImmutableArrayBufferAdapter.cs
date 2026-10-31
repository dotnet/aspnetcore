// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableArrayBufferAdapter<TElement> : ICollectionBufferAdapter<ImmutableArray<TElement>, ImmutableArray<TElement>.Builder, TElement>
{
    public static ImmutableArray<TElement>.Builder CreateBuffer() => ImmutableArray.CreateBuilder<TElement>();

    public static ImmutableArray<TElement>.Builder Add(ref ImmutableArray<TElement>.Builder buffer, TElement element)
    {
        buffer.Add(element);
        return buffer;
    }

    public static ImmutableArray<TElement> ToResult(ImmutableArray<TElement>.Builder buffer) => buffer.ToImmutable();
}
