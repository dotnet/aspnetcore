// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <inheritdoc/>
public class TempData : ITempData
{
    private readonly Dictionary<string, object?> _data = new();
    private readonly HashSet<string> _retainedKeys = new();

    /// <inheritdoc/>
    public object? this[string key]
    {
        get => Get(key);
        set => _data[key] = value;
    }

    /// <inheritdoc/>
    public object? Get(string key)
    {
        return _data.TryGetValue(key, out var value) ? value : null;
    }

    /// <inheritdoc/>
    public object? Peek(string key)
    {
        return _data.TryGetValue(key, out var value) ? value : null;
    }

    /// <inheritdoc/>
    public void Keep()
    {
        foreach (var key in _data.Keys)
        {
            _retainedKeys.Add(key);
        }
    }

    /// <inheritdoc/>
    public void Keep(string key)
    {
        if (_data.ContainsKey(key))
        {
            _retainedKeys.Add(key);
        }
    }
}
