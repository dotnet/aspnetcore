// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <inheritdoc />
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DictionaryDebugView<string, object?>))]
public class TempDataDictionary : ITempDataDictionary
{
    // Perf: Everything here is lazy because the TempDataDictionary is frequently created and passed around
    // without being manipulated.
    private Dictionary<string, object?>? _data;
    private bool _loaded;
    private readonly ITempDataProvider _provider;
    private readonly HttpContext _context;
    private HashSet<string>? _initialKeys;
    private HashSet<string>? _retainedKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempDataDictionary"/> class.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="provider">The <see cref="ITempDataProvider"/> used to Load and Save data.</param>
    public TempDataDictionary(HttpContext context, ITempDataProvider provider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(provider);

        _provider = provider;
        _loaded = false;
        _context = context;
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            Load();
            return _data.Count;
        }
    }

    /// <inheritdoc/>
    public ICollection<string> Keys
    {
        get
        {
            Load();
            return _data.Keys;
        }
    }

    /// <inheritdoc/>
    public ICollection<object?> Values
    {
        get
        {
            Load();
            return _data.Values;
        }
    }

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly
    {
        get
        {
            Load();
            return ((ICollection<KeyValuePair<string, object?>>)_data).IsReadOnly;
        }
    }

    /// <inheritdoc/>
    public object? this[string key]
    {
        get
        {
            Load();
            if (TryGetValue(key, out var value))
            {
                // Mark the key for deletion since it is read.
                _initialKeys.Remove(key);
                return value;
            }
            return null;
        }
        set
        {
            Load();
            _data[key] = value;
            _initialKeys.Add(key);
        }
    }

    /// <inheritdoc />
    public void Keep()
    {
        // if the data is not loaded, we can assume none of it has been read
        // and so silently return.
        if (!_loaded)
        {
            return;
        }

        AssertLoaded();

        _retainedKeys.Clear();
        _retainedKeys.UnionWith(_data.Keys);
    }

    /// <inheritdoc />
    public void Keep(string key)
    {
        Load();
        _retainedKeys.Add(key);
    }

    /// <inheritdoc />
    [MemberNotNull(nameof(_initialKeys), nameof(_retainedKeys), nameof(_data))]
    public void Load()
    {
        if (_loaded)
        {
            AssertLoaded();
            return;
        }

        var providerDictionary = _provider.LoadTempData(_context);
        _data = (providerDictionary != null)
            ? new Dictionary<string, object?>(providerDictionary, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _initialKeys = new HashSet<string>(_data.Keys, StringComparer.OrdinalIgnoreCase);
        _retainedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _loaded = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MemberNotNull(nameof(_initialKeys), nameof(_retainedKeys), nameof(_data))]
    private void AssertLoaded()
    {
        Debug.Assert(_initialKeys is not null && _retainedKeys is not null && _data is not null);
    }

    /// <inheritdoc />
    public void Save()
    {
        if (!_loaded)
        {
            return;
        }

        AssertLoaded();

        // In .NET Core 3.0 a Dictionary can have items removed during enumeration
        // https://github.com/dotnet/coreclr/pull/18854
        foreach (var entry in _data)
        {
            if (!_initialKeys.Contains(entry.Key) && !_retainedKeys.Contains(entry.Key))
            {
                _data.Remove(entry.Key);
            }
        }

        _provider.SaveTempData(_context, _data);
    }

    /// <inheritdoc />
    public object? Peek(string key)
    {
        Load();
        _data.TryGetValue(key, out var value);
        return value;
    }

    /// <inheritdoc/>
    public void Add(string key, object? value)
    {
        Load();
        _data.Add(key, value);
        _initialKeys.Add(key);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        Load();
        _data.Clear();
        _retainedKeys.Clear();
        _initialKeys.Clear();
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        Load();
        return _data.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool ContainsValue(object? value)
    {
        Load();
        return _data.ContainsValue(value);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        Load();
        return new TempDataDictionaryEnumerator(this);
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        Load();
        _retainedKeys.Remove(key);
        _initialKeys.Remove(key);
        return _data.Remove(key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, out object? value)
    {
        Load();
        // Mark the key for deletion since it is read.
        _initialKeys.Remove(key);
        return _data.TryGetValue(key, out value);
    }

    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int index)
    {
        Load();
        ((ICollection<KeyValuePair<string, object?>>)_data).CopyTo(array, index);
    }

    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> keyValuePair)
    {
        Load();
        _initialKeys.Add(keyValuePair.Key);
        ((ICollection<KeyValuePair<string, object?>>)_data).Add(keyValuePair);
    }

    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> keyValuePair)
    {
        Load();
        return ((ICollection<KeyValuePair<string, object?>>)_data).Contains(keyValuePair);
    }

    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> keyValuePair)
    {
        Load();
        _initialKeys.Remove(keyValuePair.Key);
        return ((ICollection<KeyValuePair<string, object?>>)_data).Remove(keyValuePair);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        Load();
        return new TempDataDictionaryEnumerator(this);
    }

    private sealed class TempDataDictionaryEnumerator : IEnumerator<KeyValuePair<string, object?>>
    {
        // Do not make this readonly. This prevents MoveNext from functioning.
        private Dictionary<string, object?>.Enumerator _enumerator;
        private readonly TempDataDictionary _tempData;

        public TempDataDictionaryEnumerator(TempDataDictionary tempData)
        {
            _tempData = tempData;
            _tempData.AssertLoaded();
            _enumerator = _tempData._data.GetEnumerator();
        }

        public KeyValuePair<string, object?> Current
        {
            get
            {
                var kvp = _enumerator.Current;
                _tempData.AssertLoaded();
                // Mark the key for deletion since it is read.
                _tempData._initialKeys.Remove(kvp.Key);
                return kvp;
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            ((IEnumerator<KeyValuePair<string, object?>>)_enumerator).Reset();
        }

        void IDisposable.Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
