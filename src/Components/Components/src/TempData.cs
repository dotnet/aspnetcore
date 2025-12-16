// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.AspNetCore.Components;

/// <inheritdoc/>
public class TempData : ITempData
{
    private readonly Dictionary<string, object?> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _retainedKeys = new(StringComparer.OrdinalIgnoreCase);

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
        _retainedKeys.Clear();
        _retainedKeys.UnionWith(_data.Keys);
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
    public bool ContainsKey(string key)
    {
        return _data.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        _retainedKeys.Remove(key);
        return _data.Remove(key);
    }

    /// <summary>
    /// Returns true if the TempData dictionary contains the specified <paramref name="value"/>.
    /// </summary>
    public bool ContainsValue(object? value)
    {
        return _data.ContainsValue(value);
    }

    /// <summary>
    /// Gets the data that should be saved for the next request.
    /// </summary>
    public IDictionary<string, object?> Save()
    {
        var dataToSave = new Dictionary<string, object?>();
        foreach (var key in _retainedKeys)
        {
            dataToSave[key] = _data[key];
        }
        return dataToSave;
    }

    /// <summary>
    /// Loads data from a <paramref name="data"/> into the TempData dictionary.
    /// </summary>
    public void Load(IDictionary<string, object?> data)
    {
        _data.Clear();
        _retainedKeys.Clear();
        foreach (var kvp in data)
        {
            _data[kvp.Key] = kvp.Value;
            _retainedKeys.Add(kvp.Key);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _data.Clear();
        _retainedKeys.Clear();
    }

    ICollection<string> IDictionary<string, object?>.Keys => _data.Keys;

    ICollection<object?> IDictionary<string, object?>.Values => _data.Values;

    int ICollection<KeyValuePair<string, object?>>.Count => _data.Count;

    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => ((ICollection<KeyValuePair<string, object?>>)_data).IsReadOnly;

    void IDictionary<string, object?>.Add(string key, object? value)
    {
        this[key] = value;
    }

    bool IDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        value = Get(key);
        return ContainsKey(key);
    }

    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item)
    {
        ((IDictionary<string, object?>)this).Add(item.Key, item.Value);
    }

    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
    {
        return ContainsKey(item.Key) && Equals(Peek(item.Key), item.Value);
    }

    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, object?>>)_data).CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
    {
        if (ContainsKey(item.Key) && Equals(Peek(item.Key), item.Value))
        {
            return Remove(item.Key);
        }
        return false;
    }

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        return new TempDataEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new TempDataEnumerator(this);
    }

    class TempDataEnumerator : IEnumerator<KeyValuePair<string, object?>>
    {
        private readonly TempData _tempData;
        private readonly IEnumerator<KeyValuePair<string, object?>> _innerEnumerator;

        public TempDataEnumerator(TempData tempData)
        {
            _tempData = tempData;
            _innerEnumerator = tempData._data.GetEnumerator();
        }

        public KeyValuePair<string, object?> Current
        {
            get
            {
                var kvp = _innerEnumerator.Current;
                _tempData.Remove(kvp.Key);
                return kvp;
            }
        }

        object IEnumerator.Current => _innerEnumerator.Current;

        public void Dispose()
        {
            _innerEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return _innerEnumerator.MoveNext();
        }

        public void Reset()
        {
            _innerEnumerator.Reset();
        }
    }
}
