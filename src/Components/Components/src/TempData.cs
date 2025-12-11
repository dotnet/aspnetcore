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
        set
        {
            _data[key] = value;
            _retainedKeys.Add(key);
        }
    }

    /// <inheritdoc/>
    public object? Get(string key)
    {
        _retainedKeys.Remove(key);
        return _data.GetValueOrDefault(key);
    }

    /// <inheritdoc/>
    public object? Peek(string key)
    {
        return _data.GetValueOrDefault(key);
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

    /// <inheritdoc/>
    public IDictionary<string, object?> GetDataToSave()
    {
        var dataToSave = new Dictionary<string, object?>();
        foreach (var key in _retainedKeys)
        {
            dataToSave[key] = _data[key];
        }
        return dataToSave;
    }

    /// <inheritdoc/>
    public void LoadDataFromCookie(IDictionary<string, object?> data)
    {
        _data.Clear();
        _retainedKeys.Clear();
        foreach (var kvp in data)
        {
            _data[kvp.Key] = kvp.Value;
            _retainedKeys.Add(kvp.Key);
        }
    }
}
