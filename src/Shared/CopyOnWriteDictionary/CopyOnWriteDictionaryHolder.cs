// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Internal;

internal struct CopyOnWriteDictionaryHolder<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _source;
    private Dictionary<TKey, TValue>? _copy;

    public CopyOnWriteDictionaryHolder(Dictionary<TKey, TValue> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _copy = null;
    }

    public CopyOnWriteDictionaryHolder(CopyOnWriteDictionaryHolder<TKey, TValue> source)
    {
        _source = source._copy ?? source._source;
        _copy = null;
    }

    public bool HasBeenCopied => _copy != null;

    public Dictionary<TKey, TValue> ReadDictionary
    {
        get
        {
            if (_copy != null)
            {
                return _copy;
            }

            if (_source != null)
            {
                return _source;
            }

            // Default-Constructor case
            _copy = new Dictionary<TKey, TValue>();
            return _copy;
        }
    }

    public Dictionary<TKey, TValue> WriteDictionary
    {
        get
        {
            _copy = _copy switch
            {
                null when _source == null => new Dictionary<TKey, TValue>(),  // Default-Constructor case
                null => new Dictionary<TKey, TValue>(_source, _source.Comparer),
                _ => _copy
            };

            return _copy;
        }
    }

    public Dictionary<TKey, TValue>.KeyCollection Keys => ReadDictionary.Keys;

    public Dictionary<TKey, TValue>.ValueCollection Values => ReadDictionary.Values;

    public int Count => ReadDictionary.Count;

    public static bool IsReadOnly => false;

    public TValue this[TKey key]
    {
        get => ReadDictionary[key];
        set => WriteDictionary[key] = value;
    }

    public bool ContainsKey(TKey key)
    {
        return ReadDictionary.ContainsKey(key);
    }

    public void Add(TKey key, TValue value)
    {
        WriteDictionary.Add(key, value);
    }

    public bool Remove(TKey key)
    {
        return WriteDictionary.Remove(key);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return ReadDictionary.TryGetValue(key, out value);
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)WriteDictionary).Add(item);
    }

    public void Clear()
    {
        WriteDictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)ReadDictionary).Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)ReadDictionary).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)WriteDictionary).Remove(item);
    }

    public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
    {
        return ReadDictionary.GetEnumerator();
    }
}
