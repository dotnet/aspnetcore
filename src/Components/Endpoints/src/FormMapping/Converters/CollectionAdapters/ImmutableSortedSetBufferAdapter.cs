// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableSortedSetBufferAdapter<TElement> : ICollectionBufferAdapter<ImmutableSortedSet<TElement>, ImmutableSortedSet<TElement>.Builder, TElement>
{
    public static ImmutableSortedSet<TElement>.Builder CreateBuffer() => ImmutableSortedSet.CreateBuilder<TElement>();

    public static ImmutableSortedSet<TElement>.Builder Add(ref ImmutableSortedSet<TElement>.Builder buffer, TElement element)
    {
        buffer.Add(element);
        return buffer;
    }

    public static ImmutableSortedSet<TElement> ToResult(ImmutableSortedSet<TElement>.Builder buffer) => buffer.ToImmutable();
}
