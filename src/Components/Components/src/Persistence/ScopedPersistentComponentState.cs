// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// TODO: Docs
/// </summary>
public class ScopedPersistentComponentState : IPersistentComponentState
{
    private readonly PersistentScope _scope;
    private readonly PersistentComponentState _state;

    internal ScopedPersistentComponentState(PersistentScope scope, PersistentComponentState state)
    {
        _scope = scope;
        _state = state;
    }
    /// <inheritdoc/>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public void PersistAsJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, TValue instance)
    {
        ArgumentNullException.ThrowIfNull(key);

        _state.PersistAsJson(_scope.ComputeScopedKey(key), instance);
    }
    /// <inheritdoc/>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public bool TryTakeFromJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, [MaybeNullWhen(false)] out TValue? instance)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _state.TryTakeFromJson(_scope.ComputeScopedKey(key), out instance);
    }
}
