// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Per-type serialization/deserialization support for <see cref="HybridCache"/>.
/// </summary>
/// <typeparam name="T">The type being serialized/deserialized.</typeparam>
public interface IHybridCacheSerializer<T>
{
    /// <summary>
    /// Deserialize a <typeparamref name="T"/> value from the provided <paramref name="source"/>.
    /// </summary>
    T Deserialize(ReadOnlySequence<byte> source);

    /// <summary>
    /// Serialize <paramref name="value"/>, writing to the provided <paramref name="target"/>.
    /// </summary>
    void Serialize(T value, IBufferWriter<byte> target);
}

