// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Diagnostics;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A dictionary for HTML attributes.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DictionaryDebugView<string, string?>))]
public class AttributeDictionary : IDictionary<string, string?>, IReadOnlyDictionary<string, string?>
{
    private List<KeyValuePair<string, string?>>? _items;

    /// <inheritdoc />
    public string? this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            var index = Find(key);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }
            else
            {
                return Get(index).Value;
            }
        }

        set
        {
            ArgumentNullException.ThrowIfNull(key);

            var item = new KeyValuePair<string, string?>(key, value);
            var index = Find(key);
            if (index < 0)
            {
                Insert(~index, item);
            }
            else
            {
                Set(index, item);
            }
        }
    }

    /// <inheritdoc />
    public int Count => _items == null ? 0 : _items.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public ICollection<string> Keys => new KeyCollection(this);

    /// <inheritdoc />
    public ICollection<string?> Values => new ValueCollection(this);

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, string?>.Keys => new KeyCollection(this);

    /// <inheritdoc />
    IEnumerable<string?> IReadOnlyDictionary<string, string?>.Values => new ValueCollection(this);

    private KeyValuePair<string, string?> Get(int index)
    {
        Debug.Assert(index >= 0 && index < Count && _items != null);
        return _items[index];
    }

    private void Set(int index, KeyValuePair<string, string?> value)
    {
        Debug.Assert(index >= 0 && index <= Count);
        Debug.Assert(value.Key != null);

        if (_items == null)
        {
            _items = new List<KeyValuePair<string, string?>>();
        }

        _items[index] = value;
    }

    private void Insert(int index, KeyValuePair<string, string?> value)
    {
        Debug.Assert(index >= 0 && index <= Count);
        Debug.Assert(value.Key != null);

        if (_items == null)
        {
            _items = new List<KeyValuePair<string, string?>>();
        }

        _items.Insert(index, value);
    }

    private void Remove(int index)
    {
        Debug.Assert(index >= 0 && index < Count);

        Debug.Assert(_items != null);
        _items.RemoveAt(index);
    }

    // This API is a lot like List<T>.BinarySearch https://msdn.microsoft.com/en-us/library/3f90y839(v=vs.110).aspx
    // If an item is not found, we return the compliment of where it belongs. Then we don't need to search again
    // to do something with it.
    private int Find(string key)
    {
        Debug.Assert(key != null);

        if (Count == 0)
        {
            return ~0;
        }

        var start = 0;
        var end = Count - 1;

        while (start <= end)
        {
            var pivot = start + (end - start >> 1);

            var compare = StringComparer.OrdinalIgnoreCase.Compare(Get(pivot).Key, key);
            if (compare == 0)
            {
                return pivot;
            }
            if (compare < 0)
            {
                start = pivot + 1;
            }
            else
            {
                end = pivot - 1;
            }
        }

        return ~start;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _items?.Clear();
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, string?> item)
    {
        if (item.Key == null)
        {
            throw new ArgumentException(
                Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(KeyValuePair<string, string?>.Key),
                    nameof(KeyValuePair<string, string?>)),
                nameof(item));
        }

        var index = Find(item.Key);
        if (index < 0)
        {
            Insert(~index, item);
        }
        else
        {
            throw new InvalidOperationException(Resources.FormatDictionary_DuplicateKey(item.Key));
        }
    }

    /// <inheritdoc />
    public void Add(string key, string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        Add(new KeyValuePair<string, string?>(key, value));
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, string?> item)
    {
        if (item.Key == null)
        {
            throw new ArgumentException(
                Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(KeyValuePair<string, string?>.Key),
                    nameof(KeyValuePair<string, string?>)),
                nameof(item));
        }

        var index = Find(item.Key);
        if (index < 0)
        {
            return false;
        }
        else
        {
            return string.Equals(item.Value, Get(index).Value, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (Count == 0)
        {
            return false;
        }

        return Find(key) >= 0;
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0 || arrayIndex >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        for (var i = 0; i < Count; i++)
        {
            array[arrayIndex + i] = Get(i);
        }
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, string?> item)
    {
        if (item.Key == null)
        {
            throw new ArgumentException(
                Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(KeyValuePair<string, string?>.Key),
                    nameof(KeyValuePair<string, string?>)),
                nameof(item));
        }

        var index = Find(item.Key);
        if (index < 0)
        {
            return false;
        }
        else if (string.Equals(item.Value, Get(index).Value, StringComparison.OrdinalIgnoreCase))
        {
            Remove(index);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var index = Find(key);
        if (index < 0)
        {
            return false;
        }
        else
        {
            Remove(index);
            return true;
        }
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var index = Find(key);
        if (index < 0)
        {
            value = null;
            return false;
        }
        else
        {
            value = Get(index).Value;
            return true;
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// An enumerator for <see cref="AttributeDictionary"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, string?>>
    {
        private readonly AttributeDictionary _attributes;

        private int _index;

        /// <summary>
        /// Creates a new <see cref="Enumerator"/>.
        /// </summary>
        /// <param name="attributes">The <see cref="AttributeDictionary"/>.</param>
        public Enumerator(AttributeDictionary attributes)
        {
            _attributes = attributes;

            _index = -1;
        }

        /// <inheritdoc />
        public KeyValuePair<string, string?> Current => _attributes.Get(_index);

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            _index++;
            return _index < _attributes.Count;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = -1;
        }
    }

    private sealed class KeyCollection : ICollection<string>
    {
        private readonly AttributeDictionary _attributes;

        public KeyCollection(AttributeDictionary attributes)
        {
            _attributes = attributes;
        }

        public int Count => _attributes.Count;

        public bool IsReadOnly => true;

        public void Add(string item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(string item)
        {
            ArgumentNullException.ThrowIfNull(item);

            for (var i = 0; i < _attributes.Count; i++)
            {
                if (string.Equals(item, _attributes.Get(i).Key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            for (var i = 0; i < _attributes.Count; i++)
            {
                array[arrayIndex + i] = _attributes.Get(i).Key;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_attributes);
        }

        public bool Remove(string item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<string?>
        {
            private readonly AttributeDictionary _attributes;

            private int _index;

            public Enumerator(AttributeDictionary attributes)
            {
                _attributes = attributes;

                _index = -1;
            }

            public string Current => _attributes.Get(_index).Key;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _attributes.Count;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }

    private sealed class ValueCollection : ICollection<string?>
    {
        private readonly AttributeDictionary _attributes;

        public ValueCollection(AttributeDictionary attributes)
        {
            _attributes = attributes;
        }

        public int Count => _attributes.Count;

        public bool IsReadOnly => true;

        public void Add(string? item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(string? item)
        {
            for (var i = 0; i < _attributes.Count; i++)
            {
                if (string.Equals(item, _attributes.Get(i).Value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(string?[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            for (var i = 0; i < _attributes.Count; i++)
            {
                array[arrayIndex + i] = _attributes.Get(i).Value;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_attributes);
        }

        public bool Remove(string? item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<string?> IEnumerable<string?>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<string?>
        {
            private readonly AttributeDictionary _attributes;

            private int _index;

            public Enumerator(AttributeDictionary attributes)
            {
                _attributes = attributes;

                _index = -1;
            }

            public string? Current => _attributes.Get(_index).Value;

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _attributes.Count;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}
