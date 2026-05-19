// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableHashSetBufferAdapter<TElement> : ICollectionBufferAdapter<ImmutableHashSet<TElement>, ImmutableHashSet<TElement>.Builder, TElement>
{
    public static ImmutableHashSet<TElement>.Builder CreateBuffer() => ImmutableHashSet.CreateBuilder<TElement>();

    public static ImmutableHashSet<TElement>.Builder Add(ref ImmutableHashSet<TElement>.Builder buffer, TElement element)
    {
        buffer.Add(element);
        return buffer;
    }

    public static ImmutableHashSet<TElement> ToResult(ImmutableHashSet<TElement>.Builder buffer) => buffer.ToImmutable();

    public static CollectionConverter<IImmutableSet<TElement>> CreateInterfaceConverter(FormDataConverter<TElement> elementConverter)
    {
        return new CollectionConverter<
            IImmutableSet<TElement>,
            StaticCastAdapter<
                IImmutableSet<TElement>,
                ImmutableHashSet<TElement>,
                ImmutableHashSetBufferAdapter<TElement>,
                ImmutableHashSet<TElement>.Builder,
                TElement>,
            ImmutableHashSet<TElement>.Builder,
            TElement>(elementConverter);
    }
}
