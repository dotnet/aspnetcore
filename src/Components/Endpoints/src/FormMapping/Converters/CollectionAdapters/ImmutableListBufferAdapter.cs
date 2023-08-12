// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableListBufferAdapter<TElement> : ICollectionBufferAdapter<ImmutableList<TElement>, ImmutableList<TElement>.Builder, TElement>
{
    public static ImmutableList<TElement>.Builder CreateBuffer() => ImmutableList.CreateBuilder<TElement>();

    public static ImmutableList<TElement>.Builder Add(ref ImmutableList<TElement>.Builder buffer, TElement element)
    {
        buffer.Add(element);
        return buffer;
    }

    public static ImmutableList<TElement> ToResult(ImmutableList<TElement>.Builder buffer) => buffer.ToImmutable();

    public static CollectionConverter<IImmutableList<TElement>> CreateInterfaceConverter(FormDataConverter<TElement> elementConverter)
    {
        return new CollectionConverter<
            IImmutableList<TElement>,
            StaticCastAdapter<
                IImmutableList<TElement>,
                ImmutableList<TElement>,
                ImmutableListBufferAdapter<TElement>,
                ImmutableList<TElement>.Builder,
                TElement>,
            ImmutableList<TElement>.Builder,
            TElement>(elementConverter);
    }
}
