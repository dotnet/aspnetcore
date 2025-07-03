// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides custom serialization logic for persistent component state values.
/// </summary>
public interface IPersistentComponentStateSerializer
{
    /// <summary>
    /// Serializes the provided <paramref name="value"/> and writes it to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="type">The type of the value to serialize.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="writer">The buffer writer to write the serialized data to.</param>
    /// <returns>A task that represents the asynchronous serialization operation.</returns>
    Task PersistAsync(Type type, object value, IBufferWriter<byte> writer);

    /// <summary>
    /// Deserializes a value from the provided <paramref name="data"/>.
    /// This method must be synchronous to avoid UI tearing during component state restoration.
    /// </summary>
    /// <param name="type">The type of the value to deserialize.</param>
    /// <param name="data">The serialized data to deserialize.</param>
    /// <returns>The deserialized value.</returns>
    object Restore(Type type, ReadOnlySequence<byte> data);
}

/// <summary>
/// Provides custom serialization logic for persistent component state values of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value to serialize.</typeparam>
public interface IPersistentComponentStateSerializer<T> : IPersistentComponentStateSerializer
{
    /// <summary>
    /// Serializes the provided <paramref name="value"/> and writes it to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="writer">The buffer writer to write the serialized data to.</param>
    /// <returns>A task that represents the asynchronous serialization operation.</returns>
    Task PersistAsync(T value, IBufferWriter<byte> writer);

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T"/> from the provided <paramref name="data"/>.
    /// This method must be synchronous to avoid UI tearing during component state restoration.
    /// </summary>
    /// <param name="data">The serialized data to deserialize.</param>
    /// <returns>The deserialized value.</returns>
    T Restore(ReadOnlySequence<byte> data);

    /// <summary>
    /// Default implementation of the non-generic PersistAsync method.
    /// </summary>
    Task IPersistentComponentStateSerializer.PersistAsync(Type type, object value, IBufferWriter<byte> writer)
        => PersistAsync((T)value, writer);

    /// <summary>
    /// Default implementation of the non-generic Restore method.
    /// </summary>
    object IPersistentComponentStateSerializer.Restore(Type type, ReadOnlySequence<byte> data)
        => Restore(data)!;
}