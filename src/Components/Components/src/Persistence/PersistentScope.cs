// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// TODO: Docs
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class PersistentScope
{
    internal PersistentScope(
        PersistentScope parentScope,
        string name,
        Action<IComponent> tracker,
        PersistentComponentState state)
    {
        Name = name;
        _state = state;
        _tracker = tracker;
        _parentScope = parentScope;
    }

    internal string Name { get; }

    private readonly PersistentComponentState _state;
    private readonly PersistentScope? _parentScope;
    private readonly Action<IComponent> _tracker;
    private HashSet<IHandleComponentPersistentState>? _registeredComponents;

    /// <summary>
    /// TODO: Docs
    /// </summary>
    public void Register(IHandleComponentPersistentState component)
    {
        _registeredComponents ??= new();
        _registeredComponents.Add(component);
        _tracker(component);
    }

    internal void Unregister(IHandleComponentPersistentState component) => _registeredComponents?.Remove(component);

    /// <summary>
    /// TODO: Docs
    /// </summary>
    public void Restore(IHandleComponentPersistentState component) =>
        component.RestoreState(new ScopedPersistentComponentState(this, _state));

    internal void ClearCallbacks() => _registeredComponents?.Clear();

    internal bool HasTrackedComponents => _registeredComponents?.Count > 0;

    internal IEnumerable<IHandleComponentPersistentState> GetRegisteredComponents() => _registeredComponents != null ? _registeredComponents : Array.Empty<IHandleComponentPersistentState>();

    private string GetDebuggerDisplay() => ComputeScopedKey("[Debug]");

    // TODO: This needs a proper implementation
    internal string ComputeScopedKey(string key)
    {
        ArgumentOutOfRangeException.ThrowIfNullOrEmpty(key, nameof(key));

        return _parentScope switch
        {
            null => $"{Name}>{key}",
            not null => _parentScope.ComputeScopedKey($"{Name}>{key}")
        };
    }
}
