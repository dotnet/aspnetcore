// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.AspNetCore.Components;

/// <inheritdoc/>
internal sealed class TempData : ITempData
{
    public bool WasLoaded => _loaded && _loadFunc is null;
    private readonly Dictionary<string, (object? Value, Type? Type)> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _retainedKeys = new(StringComparer.OrdinalIgnoreCase);
    private Func<IDictionary<string, (object? Value, Type? Type)>>? _loadFunc;
    private bool _loaded;

    internal TempData(Func<IDictionary<string, (object? Value, Type? Type)>> loadFunc)
    {
        _loadFunc = loadFunc;
    }

    private Dictionary<string, (object? Value, Type? Type)> Data
    {
        get
        {
            if (!_loaded && _loadFunc is not null)
            {
                var dataToLoad = _loadFunc();
                Load(dataToLoad);
                _loadFunc = null;
                _loaded = true;
            }
            return _data;
        }
    }

    public object? this[string key]
    {
        get
        {
            return Get(key);
        }
        set
        {
            Data[key] = (value, value?.GetType());
            _retainedKeys.Add(key);
        }
    }

    public object? Get(string key)
    {
        _retainedKeys.Remove(key);
        return Data.TryGetValue(key, out var entry) ? entry.Value : null;
    }

    public object? Peek(string key)
    {
        return Data.TryGetValue(key, out var entry) ? entry.Value : null;
    }

    public void Keep()
    {
        _retainedKeys.UnionWith(_data.Keys);
    }

    public void Keep(string key)
    {
        if (Data.ContainsKey(key))
        {
            _retainedKeys.Add(key);
        }
    }

    public bool ContainsKey(string key)
    {
        return Data.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        _retainedKeys.Remove(key);
        return Data.Remove(key);
    }

    public IDictionary<string, (object? Value, Type? Type)> Save()
    {
        var dataToSave = new Dictionary<string, (object? Value, Type? Type)>();
        foreach (var key in _retainedKeys)
        {
            dataToSave[key] = _data[key];
        }
        return dataToSave;
    }

    public void Load(IDictionary<string, (object? Value, Type? Type)> data)
    {
        _data.Clear();
        _retainedKeys.Clear();
        foreach (var kvp in data)
        {
            _data[kvp.Key] = kvp.Value;
            _retainedKeys.Add(kvp.Key);
        }
        _loaded = true;
    }

    public void Clear()
    {
        Data.Clear();
        _retainedKeys.Clear();
    }

    ICollection<string> IDictionary<string, object?>.Keys => Data.Keys;

    ICollection<object?> IDictionary<string, object?>.Values
    {
        get
        {
            var values = new List<object?>(Data.Count);
            foreach (var entry in Data.Values)
            {
                values.Add(entry.Value);
            }
            return values;
        }
    }

    int ICollection<KeyValuePair<string, object?>>.Count => Data.Count;
    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => ((ICollection<KeyValuePair<string, (object? Value, Type? Type)>>)Data).IsReadOnly;

    void IDictionary<string, object?>.Add(string key, object? value)
    {
        this[key] = value;
    }

    bool IDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        if (Data.TryGetValue(key, out var entry))
        {
            value = entry.Value;
            _retainedKeys.Remove(key);
            return true;
        }
        value = null;
        return false;
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
        foreach (var kvp in Data)
        {
            array[arrayIndex++] = new KeyValuePair<string, object?>(kvp.Key, kvp.Value.Value);
        }
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

    sealed class TempDataEnumerator : IEnumerator<KeyValuePair<string, object?>>
    {
        private readonly TempData _tempData;
        private readonly IEnumerator<KeyValuePair<string, (object? Value, Type? Type)>> _innerEnumerator;
        private readonly List<string> _keysToRemove = new();

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
                _keysToRemove.Add(kvp.Key);
                return new KeyValuePair<string, object?>(kvp.Key, kvp.Value.Value);
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _innerEnumerator.Dispose();
            foreach (var key in _keysToRemove)
            {
                _tempData._retainedKeys.Remove(key);
            }
        }

        public bool MoveNext()
        {
            return _innerEnumerator.MoveNext();
        }

        public void Reset()
        {
            _innerEnumerator.Reset();
            _keysToRemove.Clear();
        }
    }
}
