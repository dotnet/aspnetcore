// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Internal;

/// <summary>
/// An <see cref="IDictionary{String, Object}"/> type to hold a small amount of items (10 or less in the common case).
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
internal sealed class AdaptiveCapacityDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
{
    // Threshold for size of array to use.
    private const int DefaultArrayThreshold = 10;

    internal KeyValuePair<TKey, TValue>[]? _arrayStorage;
    private int _count;
    internal Dictionary<TKey, TValue>? _dictionaryStorage;
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>
    /// Creates an empty <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/>.
    /// </summary>
    public AdaptiveCapacityDictionary()
        : this(0, EqualityComparer<TKey>.Default)
    {
    }

    /// <summary>
    /// Creates a <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="comparer">Equality comparison.</param>
    public AdaptiveCapacityDictionary(IEqualityComparer<TKey> comparer)
        : this(0, comparer)
    {
    }

    /// <summary>
    /// Creates a <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    public AdaptiveCapacityDictionary(int capacity)
        : this(capacity, EqualityComparer<TKey>.Default)
    {
    }

    /// <summary>
    /// Creates a <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="capacity">Initial capacity.</param>
    /// <param name="comparer">Equality comparison.</param>
    public AdaptiveCapacityDictionary(int capacity, IEqualityComparer<TKey> comparer)
    {
        if (comparer is not null)
        {
            _comparer = comparer;
        }
        else
        {
            _comparer = EqualityComparer<TKey>.Default;
        }

        if (capacity == 0)
        {
            _arrayStorage = Array.Empty<KeyValuePair<TKey, TValue>>();
        }
        else if (capacity <= DefaultArrayThreshold)
        {
            _arrayStorage = new KeyValuePair<TKey, TValue>[capacity];
        }
        else
        {
            _dictionaryStorage = new Dictionary<TKey, TValue>(capacity, _comparer);
        }
    }

    /// <summary>
    /// Creates a <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/> initialized with the specified <paramref name="dict"/>.
    /// </summary>
    /// <param name="dict">A dictionary to use.
    /// </param>
    internal AdaptiveCapacityDictionary(Dictionary<TKey, TValue> dict)
    {
        _comparer = dict.Comparer;
        _dictionaryStorage = dict;
    }

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }

            TryGetValue(key, out var value);

            return value!;
        }

        set
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }

            if (_arrayStorage != null)
            {
                var index = FindIndex(key);
                if (index < 0)
                {
                    EnsureCapacity(_count + 1);
                    if (_dictionaryStorage != null)
                    {
                        _dictionaryStorage[key] = value;
                        return;
                    }
                    _arrayStorage[_count++] = new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    _arrayStorage[index] = new KeyValuePair<TKey, TValue>(key, value);
                }
                return;
            }

            _dictionaryStorage![key] = value;
        }
    }

    /// <inheritdoc />
    public int Count => _dictionaryStorage != null ? _dictionaryStorage.Count : _count;

    /// <inheritdoc />
    public IEqualityComparer<TKey> Comparer => _comparer;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    public ICollection<TKey> Keys
    {
        get
        {
            if (_arrayStorage != null)
            {
                // TODO if common operation, make keys and values
                // in separate arrays to avoid copying.
                var array = _arrayStorage;
                var keys = new TKey[_count];
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = array[i].Key;
                }

                return keys;
            }

            return _dictionaryStorage!.Keys;
        }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    public ICollection<TValue> Values
    {
        get
        {
            if (_arrayStorage != null)
            {
                // TODO if common operation, make keys and values
                // in separate arrays to avoid copying.
                var array = _arrayStorage;
                var values = new TValue[_count];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = array[i].Value;
                }

                return values;
            }

            return _dictionaryStorage!.Values;
        }
    }

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc />
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        if (_arrayStorage != null)
        {
            Add(item.Key, item.Value);
            return;
        }

        ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryStorage!).Add(item);
        return;
    }

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_arrayStorage != null)
        {
            EnsureCapacity(_count + 1);

            if (_dictionaryStorage != null)
            {
                Debug.Assert(_arrayStorage == null);
                _dictionaryStorage.Add(key, value);
                return;
            }

            if (ContainsKeyArray(key))
            {
                throw new ArgumentException($"An element with the key '{key}' already exists in the {nameof(AdaptiveCapacityDictionary<TKey, TValue>)}.", nameof(key));
            }

            _arrayStorage[_count] = new KeyValuePair<TKey, TValue>(key, value);
            _count++;
            return;
        }

        _dictionaryStorage!.Add(key, value);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _dictionaryStorage?.Clear();

        if (_count == 0)
        {
            return;
        }
        if (_arrayStorage != null)
        {
            Array.Clear(_arrayStorage, 0, _count);
            _count = 0;
        }
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        if (_dictionaryStorage != null)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryStorage).Contains(item);
        }

        return TryGetValue(item.Key, out var value) && EqualityComparer<object>.Default.Equals(value, item.Value);
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_dictionaryStorage is null)
        {
            return ContainsKeyArray(key);
        }

        return _dictionaryStorage.ContainsKey(key);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(
        KeyValuePair<TKey, TValue>[] array,
        int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if ((uint)arrayIndex > array.Length || array.Length - arrayIndex < this.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (_arrayStorage != null)
        {
            if (Count == 0)
            {
                return;
            }

            var storage = _arrayStorage;
            Array.Copy(storage, 0, array, arrayIndex, _count);
            return;
        }

        ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryStorage!).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        if (_dictionaryStorage != null)
        {
            return _dictionaryStorage.GetEnumerator();
        }

        return GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        if (_dictionaryStorage != null)
        {
            return _dictionaryStorage.GetEnumerator();
        }

        return GetEnumerator();
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        if (_arrayStorage != null)
        {
            if (Count == 0)
            {
                return false;
            }

            var index = FindIndex(item.Key);
            var array = _arrayStorage;
            if (index >= 0 && EqualityComparer<TValue>.Default.Equals(array[index].Value, item.Value))
            {
                Array.Copy(array, index + 1, array, index, _count - index);
                _count--;
                array[_count] = default;
                return true;
            }

            return false;
        }

        return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryStorage!).Remove(item);
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_arrayStorage != null)
        {
            if (Count == 0)
            {
                return false;
            }

            var index = FindIndex(key);
            if (index >= 0)
            {
                _count--;
                var array = _arrayStorage;
                Array.Copy(array, index + 1, array, index, _count - index);
                array[_count] = default;

                return true;
            }

            return false;
        }

        return _dictionaryStorage!.Remove(key);
    }

    /// <summary>
    /// Attempts to remove and return the value that has the specified key from the <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key of the element to remove and return.</param>
    /// <param name="value">When this method returns, contains the object removed from the <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/>, or <c>null</c> if key does not exist.</param>
    /// <returns>
    /// <c>true</c> if the object was removed successfully; otherwise, <c>false</c>.
    /// </returns>
    public bool Remove(TKey key, out TValue? value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_arrayStorage != null)
        {
            if (_count == 0)
            {
                value = default;
                return false;
            }

            var index = FindIndex(key);
            if (index >= 0)
            {
                _count--;
                var array = _arrayStorage;
                value = array[index].Value;
                Array.Copy(array, index + 1, array, index, _count - index);
                array[_count] = default;

                return true;
            }

            value = default;
            return false;
        }

        return _dictionaryStorage!.Remove(key, out value);
    }

    /// <summary>
    /// Attempts to the add the provided <paramref name="key"/> and <paramref name="value"/> to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns <c>true</c> if the value was added. Returns <c>false</c> if the key was already present.</returns>
    public bool TryAdd(TKey key, TValue value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_arrayStorage != null)
        {
            if (ContainsKey(key))
            {
                return false;
            }

            EnsureCapacity(Count + 1);

            if (_dictionaryStorage != null)
            {
                return _dictionaryStorage.TryAdd(key, value);
            }

            _arrayStorage[Count] = new KeyValuePair<TKey, TValue>(key, value);
            _count++;
            return true;
        }

        return _dictionaryStorage!.TryAdd(key, value);
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_arrayStorage != null)
        {
            return TryFindItem(key, out value);
        }

        return _dictionaryStorage!.TryGetValue(key, out value);
    }

    [DoesNotReturn]
    private static void ThrowArgumentNullExceptionForKey()
    {
        throw new ArgumentNullException("key");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int capacity)
    {
        if (_arrayStorage!.Length >= capacity)
        {
            return;
        }

        EnsureCapacitySlow(capacity);
    }

    private void EnsureCapacitySlow(int capacity)
    {
        Debug.Assert(_arrayStorage != null);

        if (capacity > DefaultArrayThreshold)
        {
            _dictionaryStorage = new Dictionary<TKey, TValue>(capacity, _comparer);
            foreach (var item in _arrayStorage)
            {
                _dictionaryStorage[item.Key] = item.Value;
            }

            // Clear array storage.
            _arrayStorage = null;
        }
        else
        {
            capacity = _arrayStorage.Length == 0 ? DefaultArrayThreshold : _arrayStorage.Length * 2;
            var array = new KeyValuePair<TKey, TValue>[capacity];
            if (_count > 0)
            {
                Array.Copy(_arrayStorage, 0, array, 0, _count);
            }

            _arrayStorage = array;
        }
    }
    private Span<KeyValuePair<TKey, TValue>> ArrayStorageSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(_arrayStorage is not null);
            Debug.Assert(_count <= _arrayStorage.Length);

            ref var r = ref MemoryMarshal.GetArrayDataReference(_arrayStorage);
            return MemoryMarshal.CreateSpan(ref r, _count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(TKey key)
    {
        Debug.Assert(_dictionaryStorage == null);
        Debug.Assert(_arrayStorage != null);

        if (_count > 0)
        {
            for (var i = 0; i < ArrayStorageSpan.Length; ++i)
            {
                if (_comparer.Equals(ArrayStorageSpan[i].Key, key))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryFindItem(TKey key, out TValue? value)
    {
        Debug.Assert(_dictionaryStorage == null);
        Debug.Assert(_arrayStorage != null);

        if (_count > 0)
        {
            foreach (ref var item in ArrayStorageSpan)
            {
                if (_comparer.Equals(item.Key, key))
                {
                    value = item.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsKeyArray(TKey key) => TryFindItem(key, out _);

    /// <inheritdoc />
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly AdaptiveCapacityDictionary<TKey, TValue> _dictionary;
        private int _index;
        // Don't mark this as readonly
        private Dictionary<TKey, TValue>.Enumerator? _dictionaryEnumerator;

        /// <summary>
        /// Instantiates a new enumerator with the values provided in <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">A <see cref="AdaptiveCapacityDictionary{TKey, TValue}"/>.</param>
        public Enumerator(AdaptiveCapacityDictionary<TKey, TValue> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _dictionary = dictionary;

            if (_dictionary._dictionaryStorage != null)
            {
                _dictionaryEnumerator = _dictionary._dictionaryStorage.GetEnumerator();
            }
            else
            {
                _dictionaryEnumerator = null;
            }

            Current = default;
            _index = 0;
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current { get; private set; }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Releases resources used by the <see cref="Enumerator"/>.
        /// </summary>
        public void Dispose()
        {
        }

        // Similar to the design of List<T>.Enumerator - Split into fast path and slow path for inlining friendliness
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var dictionary = _dictionary;
            if (dictionary._arrayStorage != null)
            {
                if (dictionary._count <= _index)
                {
                    return false;
                }

                Current = dictionary._arrayStorage[_index];
                _index++;
                return true;
            }
            else
            {
                var enumerator = _dictionaryEnumerator!.Value;
                var hasNext = enumerator.MoveNext();
                if (hasNext)
                {
                    Current = enumerator.Current;
                }

                _dictionaryEnumerator = enumerator;

                return hasNext;
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            Current = default;
            _index = 0;
        }
    }
}
