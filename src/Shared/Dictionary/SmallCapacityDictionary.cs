// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Internal.Dictionary
{
    /// <summary>
    /// An <see cref="IDictionary{String, Object}"/> type to hold a small amount of items (4 or less in the common case).
    /// </summary>
    internal class SmallCapacityDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        // Threshold for size of array to use.
        private static readonly int DefaultArrayThreshold = 4;

        internal KeyValuePair<TKey, TValue>[] _arrayStorage;
        private int _count;
        internal Dictionary<TKey, TValue>? _backup;
        private IEqualityComparer<TKey> _comparer;

        /// <summary>
        /// Creates a new <see cref="SmallCapacityDictionary{TKey, TValue}"/> from the provided array.
        /// The new instance will take ownership of the array, and may mutate it.
        /// </summary>
        /// <param name="items">The items array.</param>
        /// <param name="comparer">Equality comparison.</param>
        /// <returns>A new <see cref="SmallCapacityDictionary{TKey, TValue}"/>.</returns>
        public static SmallCapacityDictionary<TKey, TValue> FromArray(KeyValuePair<TKey, TValue>[] items, IEqualityComparer<TKey>? comparer = null)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            comparer = comparer ?? EqualityComparer<TKey>.Default;

            if (items.Length > DefaultArrayThreshold)
            {
                // Use dictionary for large arrays.
                var dict = new Dictionary<TKey, TValue>(items.Length, comparer);
                foreach (var item in items)
                {
                    if (item.Key != null)
                    {
                        dict[item.Key] = item.Value;
                    }
                }

                return new SmallCapacityDictionary<TKey, TValue>(comparer)
                {
                    _backup = dict
                };
            }

            // We need to compress the array by removing non-contiguous items. We
            // typically have a very small number of items to process. We don't need
            // to preserve order.
            var start = 0;
            var end = items.Length - 1;

            // We walk forwards from the beginning of the array and fill in 'null' slots.
            // We walk backwards from the end of the array end move items in non-null' slots
            // into whatever start is pointing to. O(n)
            while (start <= end)
            {
                if (items[start].Key != null)
                {
                    start++;
                }
                else if (items[end].Key != null)
                {
                    // Swap this item into start and advance
                    items[start] = items[end];
                    items[end] = default;
                    start++;
                    end--;
                }
                else
                {
                    // Both null, we need to hold on 'start' since we
                    // still need to fill it with something.
                    end--;
                }
            }

            return new SmallCapacityDictionary<TKey, TValue>()
            {
                _arrayStorage = items!,
                _count = start,
            };
        }

        /// <summary>
        /// Creates an empty <see cref="SmallCapacityDictionary{TKey, TValue}"/>.
        /// </summary>
        public SmallCapacityDictionary()
            : this(0, EqualityComparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Creates a <see cref="SmallCapacityDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="comparer">Equality comparison.</param>
        public SmallCapacityDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        /// <summary>
        /// Creates a <see cref="SmallCapacityDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        public SmallCapacityDictionary(int capacity)
            : this(capacity, EqualityComparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Creates a <see cref="SmallCapacityDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        /// <param name="comparer">Equality comparison.</param>
        public SmallCapacityDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (comparer is not null && comparer != EqualityComparer<TKey>.Default) // first check for null to avoid forcing default comparer instantiation unnecessarily
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
                _backup = new Dictionary<TKey, TValue>(capacity: 10);
                _arrayStorage = Array.Empty<KeyValuePair<TKey, TValue>>();
            }
        }

        /// <summary>
        /// Creates a <see cref="SmallCapacityDictionary{TKey, TValue}"/> initialized with the specified <paramref name="values"/>.
        /// </summary>
        /// <param name="values">An object to initialize the dictionary. The value can be of type
        /// <see cref="IDictionary{TKey, TValue}"/> or <see cref="IReadOnlyDictionary{TKey, TValue}"/>
        /// or an object with public properties as key-value pairs.
        /// </param>
        /// <param name="comparer">Equality comparison.</param>
        /// <param name="capacity">Initial capacity.</param>
        public SmallCapacityDictionary(IEnumerable<KeyValuePair<TKey, TValue>> values, int capacity, IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;

            _arrayStorage = new KeyValuePair<TKey, TValue>[capacity];

            foreach (var kvp in values)
            {
                Add(kvp.Key, kvp.Value);
            }
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

                if (_backup != null)
                {
                    _backup[key] = value;
                    return;
                }

                var index = FindIndex(key);
                if (index < 0)
                {
                    EnsureCapacity(_count + 1);
                    if (_backup != null)
                    {
                        _backup[key] = value;
                        return;
                    }
                    _arrayStorage[_count++] = new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    _arrayStorage[index] = new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                if (_backup != null)
                {
                    return _backup.Count;
                }
                return _count;
            }
        }

        /// <inheritdoc />
        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                return _comparer ?? EqualityComparer<TKey>.Default;
            }
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <inheritdoc />
        public ICollection<TKey> Keys
        {
            get
            {
                if (_backup != null)
                {
                    return _backup.Keys;
                }

                var array = _arrayStorage;
                var keys = new TKey[_count];
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = array[i].Key;
                }

                return keys;
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values
        {
            get
            {
                if (_backup != null)
                {
                    return _backup.Values;
                }

                var array = _arrayStorage;
                var values = new TValue[_count];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = array[i].Value;
                }

                return values;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            if (_backup != null)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)_backup).Add(item);
                return;
            }

            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }

            if (_backup != null)
            {
                _backup.Add(key, value);
                return;
            }

            EnsureCapacity(_count + 1);

            if (_backup != null)
            {
                _backup.Add(key, value);
                return;
            }

            if (ContainsKeyArray(key))
            {
                throw new ArgumentException($"An element with the key '{key}' already exists in the {nameof(SmallCapacityDictionary<TKey, TValue>)}.", nameof(key));
            }

            _arrayStorage[_count] = new KeyValuePair<TKey, TValue>(key, value);
            _count++;
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_backup != null)
            {
                _backup.Clear();
            }

            if (_count == 0)
            {
                return;
            }

            Array.Clear(_arrayStorage, 0, _count);
            _count = 0;
            _arrayStorage = Array.Empty<KeyValuePair<TKey, TValue>>();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            if (_backup != null)
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)_backup).Contains(item);
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

            if (_backup != null)
            {
                return _backup.ContainsKey(key);
            }

            return ContainsKeyCore(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ContainsKeyCore(TKey key)
        {
            return ContainsKeyArray(key);
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(
            KeyValuePair<TKey, TValue>[] array,
            int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length || array.Length - arrayIndex < this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (_backup != null)
            {
                var i = 0;
                foreach (var kvp in _backup)
                {
                    array[i] = kvp;
                    i++;
                }

                return;
            }

            if (Count == 0)
            {
                return;
            }

            var storage = _arrayStorage;
            Array.Copy(storage, 0, array, arrayIndex, _count);
        }

        /// <inheritdoc />
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            if (_backup != null)
            {
                return _backup.GetEnumerator();
            }

            return GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_backup != null)
            {
                return _backup.GetEnumerator();
            }

            return GetEnumerator();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_backup != null)
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)_backup).Remove(item);
            }

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

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }

            if (_backup != null)
            {
                return _backup.Remove(key);
            }

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

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the <see cref="SmallCapacityDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, contains the object removed from the <see cref="SmallCapacityDictionary{TKey, TValue}"/>, or <c>null</c> if key does not exist.</param>
        /// <returns>
        /// <c>true</c> if the object was removed successfully; otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(TKey key, out TValue? value)
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }


            if (_backup != null)
            {
                return _backup.Remove(key, out value);
            }

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

            if (_backup != null)
            {
                return _backup.TryAdd(key, value);
            }

            if (ContainsKeyCore(key))
            {
                return false;
            }

            EnsureCapacity(Count + 1);
            _arrayStorage[Count] = new KeyValuePair<TKey, TValue>(key, value);
            _count++;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }

            if (_backup != null)
            {
                return _backup.TryGetValue(key, out value);
            }

            return TryFindItem(key, out value);
        }


        [DoesNotReturn]
        private static void ThrowArgumentNullExceptionForKey()
        {
            throw new ArgumentNullException("key");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int capacity)
        {
            EnsureCapacitySlow(capacity);
        }

        private void EnsureCapacitySlow(int capacity)
        {
            if (_arrayStorage.Length < capacity)
            {
                if (capacity > DefaultArrayThreshold)
                {
                    _backup = new Dictionary<TKey, TValue>(capacity);
                    foreach (var item in _arrayStorage)
                    {
                        _backup[item.Key] = item.Value;
                    }

                    // Don't use _count or _arrayStorage anymore
                    // TODO clear arrays here?
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindIndex(TKey key)
        {
            // Generally the bounds checking here will be elided by the JIT because this will be called
            // on the same code path as EnsureCapacity.
            var array = _arrayStorage;
            var count = _count;

            for (var i = 0; i < count; i++)
            {
                if (_comparer.Equals(array[i].Key, key))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindItem(TKey key, out TValue? value)
        {
            var array = _arrayStorage;
            var count = _count;

            // Elide bounds check for indexing.
            if ((uint)count <= (uint)array.Length)
            {
                for (var i = 0; i < count; i++)
                {
                    if (_comparer.Equals(array[i].Key, key))
                    {
                        value = array[i].Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ContainsKeyArray(TKey key)
        {
            var array = _arrayStorage;
            var count = _count;

            // Elide bounds check for indexing.
            if ((uint)count <= (uint)array.Length)
            {
                for (var i = 0; i < count; i++)
                {
                    if (_comparer.Equals(array[i].Key, key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc />
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly SmallCapacityDictionary<TKey, TValue> _dictionary;
            private int _index;

            /// <summary>
            /// Instantiates a new enumerator with the values provided in <paramref name="dictionary"/>.
            /// </summary>
            /// <param name="dictionary">A <see cref="SmallCapacityDictionary{TKey, TValue}"/>.</param>
            public Enumerator(SmallCapacityDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }

                _dictionary = dictionary;

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
                if (dictionary._count <= _index)
                {
                    return false;
                }

                Current = dictionary._arrayStorage[_index];
                _index++;
                return true;
            }

            /// <inheritdoc />
            public void Reset()
            {
                Current = default;
                _index = 0;
            }
        }
    }
}
