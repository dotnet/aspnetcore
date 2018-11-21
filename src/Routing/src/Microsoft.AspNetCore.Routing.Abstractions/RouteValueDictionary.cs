// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Routing.Abstractions;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// An <see cref="IDictionary{String, Object}"/> type for route values.
    /// </summary>
    public class RouteValueDictionary : IDictionary<string, object>, IReadOnlyDictionary<string, object>
    {
        internal Storage _storage;

        /// <summary>
        /// Creates an empty <see cref="RouteValueDictionary"/>.
        /// </summary>
        public RouteValueDictionary()
        {
            _storage = EmptyStorage.Instance;
        }

        /// <summary>
        /// Creates a <see cref="RouteValueDictionary"/> initialized with the specified <paramref name="values"/>.
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
        public RouteValueDictionary(object values)
        {
            var dictionary = values as RouteValueDictionary;
            if (dictionary != null)
            {
                var listStorage = dictionary._storage as ListStorage;
                if (listStorage != null)
                {
                    _storage = new ListStorage(listStorage);
                    return;
                }

                var propertyStorage = dictionary._storage as PropertyStorage;
                if (propertyStorage != null)
                {
                    // PropertyStorage is immutable so we can just copy it.
                    _storage = dictionary._storage;
                    return;
                }

                // If we get here, it's an EmptyStorage.
                _storage = EmptyStorage.Instance;
                return;
            }

            var keyValueEnumerable = values as IEnumerable<KeyValuePair<string, object>>;
            if (keyValueEnumerable != null)
            {
                var listStorage = new ListStorage();
                _storage = listStorage;
                foreach (var kvp in keyValueEnumerable)
                {
                    if (listStorage.ContainsKey(kvp.Key))
                    {
                        var message = Resources.FormatRouteValueDictionary_DuplicateKey(kvp.Key, nameof(RouteValueDictionary));
                        throw new ArgumentException(message, nameof(values));
                    }

                    listStorage.Add(kvp);
                }

                return;
            }

            var stringValueEnumerable = values as IEnumerable<KeyValuePair<string, string>>;
            if (stringValueEnumerable != null)
            {
                var listStorage = new ListStorage();
                _storage = listStorage;
                foreach (var kvp in stringValueEnumerable)
                {
                    if (listStorage.ContainsKey(kvp.Key))
                    {
                        var message = Resources.FormatRouteValueDictionary_DuplicateKey(kvp.Key, nameof(RouteValueDictionary));
                        throw new ArgumentException(message, nameof(values));
                    }

                    listStorage.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                }

                return;
            }

            if (values != null)
            {
                _storage = new PropertyStorage(values);
                return;
            }

            _storage = EmptyStorage.Instance;
        }

        /// <inheritdoc />
        public object this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }

                object value;
                TryGetValue(key, out value);
                return value;
            }

            set
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (!_storage.TrySetValue(key, value))
                {
                    Upgrade();
                    _storage.TrySetValue(key, value);
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
        public int Count => _storage.Count;

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

        /// <inheritdoc />
        public ICollection<string> Keys
        {
            get
            {
                Upgrade();

                var list = (ListStorage)_storage;
                var keys = new string[list.Count];
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = list[i].Key;
                }

                return keys;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys
        {
            get
            {
                return Keys;
            }
        }

        /// <inheritdoc />
        public ICollection<object> Values
        {
            get
            {
                Upgrade();

                var list = (ListStorage)_storage;
                var values = new object[list.Count];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = list[i].Value;
                }

                return values;
            }
        }

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values
        {
            get
            {
                return Values;
            }
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Add(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Upgrade();

            var list = (ListStorage)_storage;
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    var message = Resources.FormatRouteValueDictionary_DuplicateKey(key, nameof(RouteValueDictionary));
                    throw new ArgumentException(message, nameof(key));
                }
            }

            list.Add(new KeyValuePair<string, object>(key, value));
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_storage.Count == 0)
            {
                return;
            }

            Upgrade();

            var list = (ListStorage)_storage;
            list.Clear();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            if (_storage.Count == 0)
            {
                return false;
            }

            Upgrade();

            var list = (ListStorage)_storage;
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Key, item.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return EqualityComparer<object>.Default.Equals(list[i].Value, item.Value);
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _storage.ContainsKey(key);
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, object>>.CopyTo(
            KeyValuePair<string, object>[] array,
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

            if (_storage.Count == 0)
            {
                return;
            }

            Upgrade();

            var list = (ListStorage)_storage;
            list.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            if (_storage.Count == 0)
            {
                return false;
            }

            Upgrade();

            var list = (ListStorage)_storage;
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Key, item.Key, StringComparison.OrdinalIgnoreCase) &&
                    EqualityComparer<object>.Default.Equals(list[i].Value, item.Value))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_storage.Count == 0)
            {
                return false;
            }

            Upgrade();

            var list = (ListStorage)_storage;
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _storage.TryGetValue(key, out value);
        }

        private void Upgrade()
        {
            _storage.Upgrade(ref _storage);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private readonly Storage _storage;
            private int _index;

            public Enumerator(RouteValueDictionary dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }

                _storage = dictionary._storage;

                Current = default(KeyValuePair<string, object>);
                _index = -1;
            }

            public KeyValuePair<string, object> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (++_index < _storage.Count)
                {
                    Current = _storage[_index];
                    return true;
                }

                Current = default(KeyValuePair<string, object>);
                return false;
            }

            public void Reset()
            {
                Current = default(KeyValuePair<string, object>);
                _index = -1;
            }
        }

        // Storage and its subclasses are internal for testing.
        internal abstract class Storage
        {
            public abstract int Count { get; }

            public abstract KeyValuePair<string, object> this[int index] { get; set; }

            public abstract void Upgrade(ref Storage storage);

            public abstract bool TryGetValue(string key, out object value);

            public abstract bool ContainsKey(string key);

            public abstract bool TrySetValue(string key, object value);
        }

        internal class ListStorage : Storage
        {
            private KeyValuePair<string, object>[] _items;
            private int _count;

            private static readonly KeyValuePair<string, object>[] _emptyArray = new KeyValuePair<string, object>[0];

            public ListStorage()
            {
                _items = _emptyArray;
            }

            public ListStorage(int capacity)
            {
                if (capacity == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = new KeyValuePair<string, object>[capacity];
                }
            }

            public ListStorage(ListStorage other)
            {
                if (other.Count == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = new KeyValuePair<string, object>[other.Count];
                    for (var i = 0; i < other.Count; i++)
                    {
                        this.Add(other[i]);
                    }
                }
            }

            public int Capacity => _items.Length;

            public override int Count => _count;

            public override KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index < 0 || index >= _count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return _items[index];
                }
                set
                {
                    if (index < 0 || index >= _count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    _items[index] = value;
                }
            }

            public void Add(KeyValuePair<string, object> item)
            {
                if (_count == _items.Length)
                {
                    EnsureCapacity(_count + 1);
                }

                _items[_count++] = item;
            }

            public void RemoveAt(int index)
            {
                _count--;

                for (var i = index; i < _count; i++)
                {
                    _items[i] = _items[i + 1];
                }

                _items[_count] = default(KeyValuePair<string, object>);
            }

            public void Clear()
            {
                for (var i = 0; i < _count; i++)
                {
                    _items[i] = default(KeyValuePair<string, object>);
                }

                _count = 0;
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                for (var i = 0; i < _count; i++)
                {
                    array[arrayIndex++] = _items[i];
                }
            }

            public override bool ContainsKey(string key)
            {
                for (var i = 0; i < Count; i++)
                {
                    var kvp = _items[i];
                    if (string.Equals(key, kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override bool TrySetValue(string key, object value)
            {
                for (var i = 0; i < Count; i++)
                {
                    var kvp = _items[i];
                    if (string.Equals(key, kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        _items[i] = new KeyValuePair<string, object>(key, value);
                        return true;
                    }
                }

                Add(new KeyValuePair<string, object>(key, value));
                return true;
            }

            public override bool TryGetValue(string key, out object value)
            {
                for (var i = 0; i < Count; i++)
                {
                    var kvp = _items[i];
                    if (string.Equals(key, kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = kvp.Value;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            public override void Upgrade(ref Storage storage)
            {
                // Do nothing.
            }

            private void EnsureCapacity(int min)
            {
                var newLength = _items.Length == 0 ? 4 : _items.Length * 2;
                var newItems = new KeyValuePair<string, object>[newLength];
                for (var i = 0; i < _count; i++)
                {
                    newItems[i] = _items[i];
                }

                _items = newItems;
            }
        }

        internal class PropertyStorage : Storage
        {
            private static readonly PropertyCache _propertyCache = new PropertyCache();

            internal readonly object _value;
            internal readonly PropertyHelper[] _properties;

            public PropertyStorage(object value)
            {
                Debug.Assert(value != null);
                _value = value;

                // Cache the properties so we can know if we've already validated them for duplicates.
                var type = _value.GetType();
                if (!_propertyCache.TryGetValue(type, out _properties))
                {
                    _properties = PropertyHelper.GetVisibleProperties(type);
                    ValidatePropertyNames(type, _properties);
                    _propertyCache.TryAdd(type, _properties);
                }
            }

            public PropertyStorage(PropertyStorage propertyStorage)
            {
                _value = propertyStorage._value;
                _properties = propertyStorage._properties;
            }

            public override int Count => _properties.Length;

            public override KeyValuePair<string, object> this[int index]
            {
                get
                {
                    var property = _properties[index];
                    return new KeyValuePair<string, object>(property.Name, property.GetValue(_value));
                }
                set
                {
                    // PropertyStorage never sets a value.
                    throw new NotImplementedException();
                }
            }

            public override bool TryGetValue(string key, out object value)
            {
                for (var i = 0; i < _properties.Length; i++)
                {
                    var property = _properties[i];
                    if (string.Equals(key, property.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.GetValue(_value);
                        return true;
                    }
                }

                value = null;
                return false;
            }

            public override bool ContainsKey(string key)
            {
                for (var i = 0; i < _properties.Length; i++)
                {
                    var property = _properties[i];
                    if (string.Equals(key, property.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override bool TrySetValue(string key, object value)
            {
                // PropertyStorage never sets a value.
                return false;
            }

            public override void Upgrade(ref Storage storage)
            {
                storage = new ListStorage(Count);
                for (var i = 0; i < _properties.Length; i++)
                {
                    var property = _properties[i];
                    storage.TrySetValue(property.Name, property.GetValue(_value));
                }
            }

            private static void ValidatePropertyNames(Type type, PropertyHelper[] properties)
            {
                var names = new Dictionary<string, PropertyHelper>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];

                    PropertyHelper duplicate;
                    if (names.TryGetValue(property.Name, out duplicate))
                    {
                        var message = Resources.FormatRouteValueDictionary_DuplicatePropertyName(
                            type.FullName,
                            property.Name,
                            duplicate.Name,
                            nameof(RouteValueDictionary));
                        throw new InvalidOperationException(message);
                    }

                    names.Add(property.Name, property);
                }
            }
        }

        internal class EmptyStorage : Storage
        {
            public static readonly EmptyStorage Instance = new EmptyStorage();

            private EmptyStorage()
            {
            }

            public override int Count => 0;

            public override KeyValuePair<string, object> this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override bool ContainsKey(string key)
            {
                return false;
            }

            public override bool TryGetValue(string key, out object value)
            {
                value = null;
                return false;
            }

            public override bool TrySetValue(string key, object value)
            {
                return false;
            }

            public override void Upgrade(ref Storage storage)
            {
                storage = new ListStorage();
            }
        }

        private class PropertyCache : ConcurrentDictionary<Type, PropertyHelper[]>
        {
        }
    }
}
