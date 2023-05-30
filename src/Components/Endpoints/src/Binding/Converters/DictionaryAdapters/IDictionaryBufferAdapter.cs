// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal interface IDictionaryBufferAdapter<TDictionary, TBuffer, TKey, TValue>
    where TKey : IParsable<TKey>
{
    public static abstract TBuffer CreateBuffer();

    public static abstract TBuffer Add(ref TBuffer buffer, TKey key, TValue value);

    public static abstract TDictionary ToResult(TBuffer buffer);
}
