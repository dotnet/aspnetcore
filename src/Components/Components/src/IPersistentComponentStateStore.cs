// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Manages the storage for components and services that are part of a Blazor application.
/// </summary>
public interface IPersistentComponentStateStore
{
    /// <summary>
    /// Gets the persisted state from the store.
    /// </summary>
    /// <returns>The persisted state.</returns>
    Task<IDictionary<string, byte[]>> GetPersistedStateAsync();

    /// <summary>
    /// Persists the serialized state into the storage.
    /// </summary>
    /// <param name="state">The serialized state to persist.</param>
    /// <returns>A <see cref="Task" /> that completes when the state is persisted to disk.</returns>
    Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state);

    /// <summary>
    /// Returns a value that indicates whether the store supports the given <see cref="PersistedStateSerializationMode"/>.
    /// </summary>
    /// <param name="serializationMode">The <see cref="PersistedStateSerializationMode"/> in question.</param>
    /// <returns><c>true</c> if the serialization mode is supported by the store, otherwise <c>false</c>.</returns>
    bool SupportsSerializationMode(PersistedStateSerializationMode serializationMode) => true;
}
