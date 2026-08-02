// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableSortedDictionaryBufferAdapter<TKey, TValue>
    : IDictionaryBufferAdapter<ImmutableSortedDictionary<TKey, TValue>, ImmutableSortedDictionary<TKey, TValue>.Builder, TKey, TValue>
    where TKey : IParsable<TKey>
{
    public static ImmutableSortedDictionary<TKey, TValue>.Builder Add(ref ImmutableSortedDictionary<TKey, TValue>.Builder buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuffer() => ImmutableSortedDictionary.CreateBuilder<TKey, TValue>();

    public static ImmutableSortedDictionary<TKey, TValue> ToResult(ImmutableSortedDictionary<TKey, TValue>.Builder buffer) => buffer.ToImmutable();
}
