// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

// Adapts a concrete dictionary type into an interface.
internal sealed class DictionaryStaticCastAdapter<TDictionaryInterface, TDictionaryImplementation, TDictionaryAdapter, TBuffer, TKey, TValue>
    : IDictionaryBufferAdapter<TDictionaryInterface, TBuffer, TKey, TValue>
    where TDictionaryAdapter : IDictionaryBufferAdapter<TDictionaryImplementation, TBuffer, TKey, TValue>
    where TDictionaryImplementation : TDictionaryInterface
    where TKey : IParsable<TKey>
{
    public static TBuffer CreateBuffer() => TDictionaryAdapter.CreateBuffer();

    public static TBuffer Add(ref TBuffer buffer, TKey key, TValue element) => TDictionaryAdapter.Add(ref buffer, key, element);

    public static TDictionaryInterface ToResult(TBuffer buffer) => TDictionaryAdapter.ToResult(buffer);
}
