// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ImmutableDictionaryBufferAdapter<TKey, TValue>
    : IDictionaryBufferAdapter<ImmutableDictionary<TKey, TValue>, ImmutableDictionary<TKey, TValue>.Builder, TKey, TValue>
    where TKey : ISpanParsable<TKey>
{
    public static ImmutableDictionary<TKey, TValue>.Builder Add(ref ImmutableDictionary<TKey, TValue>.Builder buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static ImmutableDictionary<TKey, TValue>.Builder CreateBuffer() => ImmutableDictionary.CreateBuilder<TKey, TValue>();

    public static ImmutableDictionary<TKey, TValue> ToResult(ImmutableDictionary<TKey, TValue>.Builder buffer) => buffer.ToImmutable();

    internal static DictionaryConverter<IImmutableDictionary<TKey, TValue>> CreateInterfaceConverter(FormDataConverter<TValue> valueTypeConverter)
    {
        return new DictionaryConverter<IImmutableDictionary<TKey, TValue>,
            DictionaryStaticCastAdapter<
                IImmutableDictionary<TKey, TValue>,
                ImmutableDictionary<TKey, TValue>,
                ImmutableDictionaryBufferAdapter<TKey, TValue>,
                ImmutableDictionary<TKey, TValue>.Builder,
                TKey,
                TValue>,
            ImmutableDictionary<TKey, TValue>.Builder,
            TKey,
            TValue>(valueTypeConverter);
    }
}
