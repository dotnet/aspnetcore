// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Factory provider for per-type <see cref="IHybridCacheSerializer{T}"/> instances.
/// </summary>
public interface IHybridCacheSerializerFactory
{
    /// <summary>
    /// Request a serializer for the provided type, if possible.
    /// </summary>
    /// <typeparam name="T">The type being serialized/deserialized.</typeparam>
    /// <param name="serializer">The serializer.</param>
    /// <returns><c>true</c> if the factory supports this type, <c>false</c> otherwise.</returns>
    bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer);
}
