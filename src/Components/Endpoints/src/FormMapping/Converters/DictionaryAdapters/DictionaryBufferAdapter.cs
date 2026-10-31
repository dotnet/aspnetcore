// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

// Uses a concrete type that implements IDictionary<TKey, TValue> as a buffer.
internal sealed class DictionaryBufferAdapter<TDictionaryType, TKey, TValue>
    : IDictionaryBufferAdapter<TDictionaryType, TDictionaryType, TKey, TValue>
    where TDictionaryType : IDictionary<TKey, TValue>, new()
    where TKey : IParsable<TKey>
{
    public static TDictionaryType Add(ref TDictionaryType buffer, TKey key, TValue value)
    {
        buffer.Add(key, value);
        return buffer;
    }

    public static TDictionaryType CreateBuffer() => new TDictionaryType();

    public static TDictionaryType ToResult(TDictionaryType buffer) => buffer;
}
