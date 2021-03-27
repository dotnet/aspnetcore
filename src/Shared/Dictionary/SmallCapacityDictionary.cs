// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Internal.Dictionary
{
    /// <summary>
    /// An <see cref="IDictionary{String, Object}"/> type for route values.
    /// </summary>
    internal class SmallCapacityDictionary : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
    {
        // Threshold for size of array to use.
        private static readonly int DefaultArrayThreshold = 4;

        internal KeyValuePair<string, object?>[] _arrayStorage;
        private int _count;
        private Dictionary<string, object?>? _backup;
        private int _threshold = DefaultArrayThreshold;

        /// <summary>
        /// Creates a new <see cref="SmallCapacityDictionary"/> from the provided array.
        /// The new instance will take ownership of the array, and may mutate it.
        /// </summary>
        /// <param name="items">The items array.</param>
        /// <returns>A new <see cref="SmallCapacityDictionary"/>.</returns>
        public static SmallCapacityDictionary FromArray(KeyValuePair<string, object?>[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Length > DefaultArrayThreshold)
            {
                // Don't use dictionary for large arrays.
                var dict = new Dictionary<string, object?>();
                foreach (var item in items)
                {
                    dict[item.Key] = item.Value;
                }

                return new SmallCapacityDictionary()
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

            return new SmallCapacityDictionary()
            {
                _arrayStorage = items!,
                _count = start,
            };
        }

        /// <summary>
        /// Creates an empty <see cref="SmallCapacityDictionary"/>.
        /// </summary>
        public SmallCapacityDictionary()
        {
            _arrayStorage = Array.Empty<KeyValuePair<string, object?>>();
        }

        public SmallCapacityDictionary(Dictionary<string, string> dict)
        {
            _backup = dict;
            _arrayStorage = Array.Empty<KeyValuePair<string, object?>>();
        }

        public SmallCapacityDictionary(int capacity)
        {
            _arrayStorage = new KeyValuePair<string, object?>[capacity];
        }

        /// <summary>
        /// Creates a <see cref="SmallCapacityDictionary"/> initialized with the specified <paramref name="values"/>.
        /// </summary>
        /// <param name="values">An object to initialize the dictionary. The value can be of type
        /// <see cref="IDictionary{TKey, TValue}"/> or <see cref="IReadOnlyDictionary{TKey, TValue}"/>
        /// or an object with public properties as key-value pairs.
        /// </param>
        /// <remarks>
        /// If the value is a dictionary or other <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{String, Object}"/>,
        /// then its entries are copied. Otherwise the object is interpreted as a set of key-value pairs where the
        /// property names are keys, and property values are the values, and copied into the dictionary.
        /// Only public instance non-index properties are considered.
        /// </remarks>
        public SmallCapacityDictionary(object? values)
        {
            if (values is SmallCapacityDictionary dictionary)
            {
                var count = dictionary._count;
                if (count > 0)
                {
                    var other = dictionary._arrayStorage;
                    var storage = new KeyValuePair<string, object?>[count];
                    Array.Copy(other, 0, storage, 0, count);
                    _arrayStorage = storage;
                    _count = count;
                }
                else
                {
                    _arrayStorage = Array.Empty<KeyValuePair<string, object?>>();
                }

                return;
            }

            if (values is IEnumerable<KeyValuePair<string, object>> keyValueEnumerable)
            {
                _arrayStorage = Array.Empty<KeyValuePair<string, object?>>();

                foreach (var kvp in keyValueEnumerable)
                {
                    Add(kvp.Key, kvp.Value);
                }

                return;
            }

            if (values is IEnumerable<KeyValuePair<string, string>> stringValueEnumerable)
            {
                _arrayStorage = Array.Empty<KeyValuePair<string, object?>>();

                foreach (var kvp in stringValueEnumerable)
                {
                    Add(kvp.Key, kvp.Value);
                }

                return;
            }

            _arrayStorage = Array.Empty<KeyValuePair<string, object?>>();
        }

        /// <inheritdoc />
        public object? this[string key]
        {
            get
            {
                if (key == null)
                {
                    ThrowArgumentNullExceptionForKey();
                }

                TryGetValue(key, out var value);
                return value;
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
                    _arrayStorage[_count++] = new KeyValuePair<string, object?>(key, value);
                }
                else
                {
                    _arrayStorage[index] = new KeyValuePair<string, object?>(key, value);
                }
            }
        }

        /// <summary>
        /// Gets the comparer for this dictionary.
        /// </summary>
        /// <remarks>
        /// This will always be a reference to <see cref="StringComparer.OrdinalIgnoreCase"/>
        /// </remarks>
        public IEqualityComparer<string> Comparer => StringComparer.OrdinalIgnoreCase;

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
        bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

        /// <inheritdoc />
        public ICollection<string> Keys
        {
            get
            {
                if (_backup != null)
                {
                    return _backup.Keys;
                }

                var array = _arrayStorage;
                var keys = new string[_count];
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = array[i].Key;
                }

                return keys;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => Keys;

        /// <inheritdoc />
        public ICollection<object?> Values
        {
            get
            {
                if (_backup != null)
                {
                    return _backup.Values;
                }

                var array = _arrayStorage;
                var values = new object?[_count];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = array[i].Value;
                }

                return values;
            }
        }

        IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => Values;

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item)
        {
            if (_backup != null)
            {
                ((ICollection<KeyValuePair<string, object?>>)_backup).Add(item);
                return;
            }

            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Add(string key, object? value)
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
                throw new ArgumentException($"An element with the key '{nameof(key)}' already exists in the {nameof(SmallCapacityDictionary)}.");
            }

            _arrayStorage[_count] = new KeyValuePair<string, object?>(key, value);
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
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
        {
            if (_backup != null)
            {
                return ((ICollection<KeyValuePair<string, object?>>)_backup).Contains(item);
            }

            return TryGetValue(item.Key, out var value) && EqualityComparer<object>.Default.Equals(value, item.Value);
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
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
        private bool ContainsKeyCore(string key)
        {
            return ContainsKeyArray(key);
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, object?>>.CopyTo(
            KeyValuePair<string, object?>[] array,
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
        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
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
        bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
        {
            if (_backup != null)
            {
                return ((ICollection<KeyValuePair<string, object?>>)_backup).Remove(item);
            }

            if (Count == 0)
            {
                return false;
            }

            var index = FindIndex(item.Key);
            var array = _arrayStorage;
            if (index >= 0 && EqualityComparer<object>.Default.Equals(array[index].Value, item.Value))
            {
                Array.Copy(array, index + 1, array, index, _count - index);
                _count--;
                array[_count] = default;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Remove(string key)
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
        /// Attempts to remove and return the value that has the specified key from the <see cref="SmallCapacityDictionary"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, contains the object removed from the <see cref="SmallCapacityDictionary"/>, or <c>null</c> if key does not exist.</param>
        /// <returns>
        /// <c>true</c> if the object was removed successfully; otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(string key, out object? value)
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
        public bool TryAdd(string key, object? value)
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
            _arrayStorage[Count] = new KeyValuePair<string, object?>(key, value);
            _count++;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object? value)
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
                if (capacity < DefaultArrayThreshold)
                {
                    _backup = new Dictionary<string, object?>(capacity);
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
                    var array = new KeyValuePair<string, object?>[capacity];
                    if (_count > 0)
                    {
                        Array.Copy(_arrayStorage, 0, array, 0, _count);
                    }

                    _arrayStorage = array;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindIndex(string key)
        {
            // Generally the bounds checking here will be elided by the JIT because this will be called
            // on the same code path as EnsureCapacity.
            var array = _arrayStorage;
            var count = _count;

            for (var i = 0; i < count; i++)
            {
                if (string.Equals(array[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindItem(string key, out object? value)
        {
            var array = _arrayStorage;
            var count = _count;

            // Elide bounds check for indexing.
            if ((uint)count <= (uint)array.Length)
            {
                for (var i = 0; i < count; i++)
                {
                    if (string.Equals(array[i].Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = array[i].Value;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ContainsKeyArray(string key)
        {
            var array = _arrayStorage;
            var count = _count;

            // Elide bounds check for indexing.
            if ((uint)count <= (uint)array.Length)
            {
                for (var i = 0; i < count; i++)
                {
                    if (string.Equals(array[i].Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc />
        public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>
        {
            private readonly SmallCapacityDictionary _dictionary;
            private int _index;

            /// <summary>
            /// Instantiates a new enumerator with the values provided in <paramref name="dictionary"/>.
            /// </summary>
            /// <param name="dictionary">A <see cref="SmallCapacityDictionary"/>.</param>
            public Enumerator(SmallCapacityDictionary dictionary)
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
            public KeyValuePair<string, object?> Current { get; private set; }

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

                // The uncommon case is that the propertyStorage is in use
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
