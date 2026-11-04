// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ReadOnlyDictionaryBufferAdapter<TKey, TValue>
    : IDictionaryBufferAdapter<ReadOnlyDictionary<TKey, TValue>, Dictionary<TKey, TValue>, TKey, TValue>
    where TKey : ISpanParsable<TKey>
{
    public static Dictionary<TKey, TValue> Add(ref Dictionary<TKey, TValue> buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static Dictionary<TKey, TValue> CreateBuffer() =>
        new Dictionary<TKey, TValue>();

    public static ReadOnlyDictionary<TKey, TValue> ToResult(Dictionary<TKey, TValue> buffer) =>
        new ReadOnlyDictionary<TKey, TValue>(buffer);

    internal static DictionaryConverter<IReadOnlyDictionary<TKey, TValue>> CreateInterfaceConverter(FormDataConverter<TValue> valueTypeConverter)
    {
        return new DictionaryConverter<IReadOnlyDictionary<TKey, TValue>,
            DictionaryStaticCastAdapter<
                IReadOnlyDictionary<TKey, TValue>,
                ReadOnlyDictionary<TKey, TValue>,
                ReadOnlyDictionaryBufferAdapter<TKey, TValue>,
                Dictionary<TKey, TValue>,
                TKey,
                TValue>,
            Dictionary<TKey, TValue>,
            TKey,
            TValue>(valueTypeConverter);
    }

    internal static DictionaryConverter<ReadOnlyDictionary<TKey, TValue>> CreateConverter(FormDataConverter<TValue> valueTypeConverter)
    {
        return new DictionaryConverter<ReadOnlyDictionary<TKey, TValue>,
            ReadOnlyDictionaryBufferAdapter<TKey, TValue>,
            Dictionary<TKey, TValue>,
            TKey,
            TValue>(valueTypeConverter);
    }
}
