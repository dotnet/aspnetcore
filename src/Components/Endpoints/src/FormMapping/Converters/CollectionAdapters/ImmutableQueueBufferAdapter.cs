// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableQueueBufferAdapter<TElement>
    : ArrayPoolBufferAdapter<ImmutableQueue<TElement>, ImmutableQueueBufferAdapter<TElement>.ImmutableQueueFactory, TElement>
{
    internal class ImmutableQueueFactory : ICollectionFactory<ImmutableQueue<TElement>, TElement>
    {
        public static ImmutableQueue<TElement> ToResultCore(TElement[] buffer, int size)
        {
            return ImmutableQueue.CreateRange(buffer.Take(size));
        }
    }

    public static CollectionConverter<IImmutableQueue<TElement>> CreateInterfaceConverter(FormDataConverter<TElement> elementConverter)
    {
        return new CollectionConverter<
            IImmutableQueue<TElement>,
            StaticCastAdapter<
                IImmutableQueue<TElement>,
                ImmutableQueue<TElement>,
                ImmutableQueueBufferAdapter<TElement>,
                PooledBuffer,
                TElement>,
            PooledBuffer,
            TElement>(elementConverter);
    }
}
