// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ReadOnlyCollectionBufferAdapter<TElement> : ICollectionBufferAdapter<ReadOnlyCollection<TElement>, IList<TElement>, TElement>
{
    public static IList<TElement> CreateBuffer() => new List<TElement>();

    public static IList<TElement> Add(ref IList<TElement> buffer, TElement element)
    {
        buffer.Add(element);
        return buffer;
    }

    public static ReadOnlyCollection<TElement> ToResult(IList<TElement> buffer) => new(buffer);
}
