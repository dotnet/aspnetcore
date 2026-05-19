// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Holds arbitrary key/value pairs associated with an <see cref="EditContext"/>.
/// This can be used to track additional metadata for application-specific purposes.
/// </summary>
public sealed class EditContextProperties
{
    // We don't want to expose any way of enumerating the underlying dictionary, because that would
    // prevent its usage to store private information. So we only expose an indexer and TryGetValue.
    private Dictionary<object, object>? _contents;

    /// <summary>
    /// Gets or sets a value in the collection.
    /// </summary>
    /// <param name="key">The key under which the value is stored.</param>
    /// <returns>The stored value.</returns>
    public object this[object key]
    {
        get => _contents is null ? throw new KeyNotFoundException() : _contents[key];
        set
        {
            _contents ??= new Dictionary<object, object>();
            _contents[key] = value;
        }
    }

    /// <summary>
    /// Gets the value associated with the specified key, if any.
    /// </summary>
    /// <param name="key">The key under which the value is stored.</param>
    /// <param name="value">The value, if present.</param>
    /// <returns>True if the value was present, otherwise false.</returns>
    public bool TryGetValue(object key, [NotNullWhen(true)] out object? value)
    {
        if (_contents is null)
        {
            value = default;
            return false;
        }
        else
        {
            return _contents.TryGetValue(key, out value);
        }
    }

    /// <summary>
    /// Removes the specified entry from the collection.
    /// </summary>
    /// <param name="key">The key of the entry to be removed.</param>
    /// <returns>True if the value was present, otherwise false.</returns>
    public bool Remove(object key)
    {
        return _contents?.Remove(key) ?? false;
    }
}
