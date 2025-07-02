// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

/// <summary>
/// A simple implementation of <see cref="IPersistentComponentStateStore"/> that stores state in a dictionary.
/// </summary>
internal sealed class DictionaryPersistentComponentStateStore : IPersistentComponentStateStore
{
    private readonly IDictionary<string, byte[]> _state;

    public DictionaryPersistentComponentStateStore(IDictionary<string, byte[]> state)
    {
        _state = state;
    }

    public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
    {
        return Task.FromResult(_state);
    }

    public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
    {
        // Not needed for WebAssembly scenarios - state is received from JavaScript
        throw new NotSupportedException("Persisting state is not supported in WebAssembly scenarios.");
    }

    public bool SupportsRenderMode(IComponentRenderMode? renderMode)
    {
        // Accept all render modes since this is just for state restoration
        return true;
    }
}