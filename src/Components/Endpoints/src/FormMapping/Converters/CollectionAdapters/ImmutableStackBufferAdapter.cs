// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableStackBufferAdapter<TElement>
    : ArrayPoolBufferAdapter<ImmutableStack<TElement>, ImmutableStackBufferAdapter<TElement>.ImmutableStackFactory, TElement>
{
    internal class ImmutableStackFactory : ICollectionFactory<ImmutableStack<TElement>, TElement>
    {
        public static ImmutableStack<TElement> ToResultCore(TElement[] buffer, int size)
        {
            return ImmutableStack.CreateRange(buffer.Take(size));
        }
    }

    public static CollectionConverter<IImmutableStack<TElement>> CreateInterfaceConverter(FormDataConverter<TElement> elementConverter)
    {
        return new CollectionConverter<
            IImmutableStack<TElement>,
            StaticCastAdapter<
                IImmutableStack<TElement>,
                ImmutableStack<TElement>,
                ImmutableStackBufferAdapter<TElement>,
                PooledBuffer,
                TElement>,
            PooledBuffer,
            TElement>(elementConverter);
    }
}

internal class StaticCastAdapter<TCollectionInterface, TCollectionImplementation, TCollectionAdapter, TBuffer, TElement>
    : ICollectionBufferAdapter<TCollectionInterface, TBuffer, TElement>
    where TCollectionAdapter : ICollectionBufferAdapter<TCollectionImplementation, TBuffer, TElement>
    where TCollectionImplementation : TCollectionInterface
{
    public static TBuffer CreateBuffer() => TCollectionAdapter.CreateBuffer();

    public static TBuffer Add(ref TBuffer buffer, TElement element) => TCollectionAdapter.Add(ref buffer, element);

    public static TCollectionInterface ToResult(TBuffer buffer) => TCollectionAdapter.ToResult(buffer);
}
