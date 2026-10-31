// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

// Interface for constructing a dictionary like instance using the
// dictionary converter.
// This interface abstracts over the different ways of constructing a dictionary.
// For example, Immutable types use a builder as a buffer, while other types
// use an instance of the dictionary itself as a buffer.
internal interface IDictionaryBufferAdapter<TDictionary, TBuffer, TKey, TValue>
    where TKey : IParsable<TKey>
{
    public static abstract TBuffer CreateBuffer();

    public static abstract TBuffer Add(ref TBuffer buffer, TKey key, TValue value);

    public static abstract TDictionary ToResult(TBuffer buffer);
}
