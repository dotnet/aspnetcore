// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components;

internal class PrerenderComponentApplicationStore : IPersistentComponentStateStore
{
    public PrerenderComponentApplicationStore()
    {
        ExistingState = new();
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Simple deserialize of primitive types.")]
    public PrerenderComponentApplicationStore(string existingState)
    {
        if (existingState is null)
        {
            throw new ArgumentNullException(nameof(existingState));
        }

        DeserializeState(Convert.FromBase64String(existingState));
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Simple deserialize of primitive types.")]
    protected void DeserializeState(byte[] existingState)
    {
        var state = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(existingState);
        if (state == null)
        {
            throw new ArgumentException("Could not deserialize state correctly", nameof(existingState));
        }

        ExistingState = state;
    }

#nullable enable
    public string? PersistedState { get; private set; }
#nullable disable

    public Dictionary<string, byte[]> ExistingState { get; protected set; }

    public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
    {
        return Task.FromResult((IDictionary<string, byte[]>)ExistingState);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Simple serialize of primitive types.")]
    protected virtual byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state) =>
        JsonSerializer.SerializeToUtf8Bytes(state);

    public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
    {
        PersistedState = Convert.ToBase64String(SerializeState(state));
        return Task.CompletedTask;
    }
}
