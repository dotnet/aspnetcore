// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.Routing
{
    /// <summary>
    /// An <see cref="IDictionary{string, object}"/> type for route values.
    /// </summary>
    public class RouteValueDictionary : IDictionary<string, object>, IReadOnlyDictionary<string, object>
    {
        /// <summary>
        /// Creates an empty <see cref="RouteValueDictionary"/>.
        /// </summary>
        public RouteValueDictionary()
        {
        }

        /// <summary>
        /// Creates a <see cref="RouteValueDictionary"/> initialized with the specified <paramref name="values"/>.
        /// </summary>
        /// <param name="values">An object to initialize the dictionary. The value can be of type
        /// <see cref="IDictionary{TKey, TValue}"/> or <see cref="IReadOnlyDictionary{TKey, TValue}"/>
        /// or an object with public properties as key-value pairs.
        /// </param>
        /// <remarks>
        /// If the value is a dictionary or other <see cref="IEnumerable{KeyValuePair{string, object}}"/>,
        /// then its entries are copied. Otherwise the object is interpreted as a set of key-value pairs where the
        /// property names are keys, and property values are the values, and copied into the dictionary.
        /// Only public instance non-index properties are considered.
        /// </remarks>
        public RouteValueDictionary(object values)
        {
            var otherDictionary = values as RouteValueDictionary;
            if (otherDictionary != null)
            {
                if (otherDictionary.InnerDictionary != null)
                {
                    InnerDictionary = new Dictionary<string, object>(
                        otherDictionary.InnerDictionary.Count,
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in otherDictionary.InnerDictionary)
                    {
                        InnerDictionary[kvp.Key] = kvp.Value;
                    }

                    return;
                }
                else if (otherDictionary.Properties != null)
                {
                    Properties = otherDictionary.Properties;
                    Value = otherDictionary.Value;
                    return;
                }
                else
                {
                    return;
                }
            }

            var keyValuePairCollection = values as IEnumerable<KeyValuePair<string, object>>;
            if (keyValuePairCollection != null)
            {
                InnerDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in keyValuePairCollection)
                {
                    InnerDictionary[kvp.Key] = kvp.Value;
                }

                return;
            }

            if (values != null)
            {
                Properties = PropertyHelper.GetVisibleProperties(values);
                Value = values;

                return;
            }
        }

        private Dictionary<string, object> InnerDictionary { get; set; }

        private PropertyHelper[] Properties { get; }

        private object Value { get; }

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

                EnsureWritable();
                InnerDictionary[key] = value;
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
        public int Count => InnerDictionary?.Count ?? Properties?.Length ?? 0;

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

        /// <inheritdoc />
        public ICollection<string> Keys
        {
            get
            {
                EnsureWritable();
                return InnerDictionary.Keys;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys
        {
            get
            {
                EnsureWritable();
                return InnerDictionary.Keys;
            }
        }

        /// <inheritdoc />
        public ICollection<object> Values
        {
            get
            {
                EnsureWritable();
                return InnerDictionary.Values;
            }
        }

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values
        {
            get
            {
                EnsureWritable();
                return InnerDictionary.Values;
            }
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            EnsureWritable();
            ((ICollection<KeyValuePair<string, object>>)InnerDictionary).Add(item);
        }

        /// <inheritdoc />
        public void Add(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureWritable();
            InnerDictionary.Add(key, value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            EnsureWritable();
            InnerDictionary.Clear();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            EnsureWritable();
            return ((ICollection<KeyValuePair<string, object>>)InnerDictionary).Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (InnerDictionary != null)
            {
                return InnerDictionary.ContainsKey(key);
            }
            else if (Properties != null)
            {
                for (var i = 0; i < Properties.Length; i++)
                {
                    var property = Properties[i];
                    if (Comparer.Equals(property.Name, key))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
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

            EnsureWritable();
            ((ICollection<KeyValuePair<string, object>>)InnerDictionary).CopyTo(array, arrayIndex);
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
            EnsureWritable();
            return ((ICollection<KeyValuePair<string, object>>)InnerDictionary).Remove(item);
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureWritable();
            return InnerDictionary.Remove(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (InnerDictionary != null)
            {
                return InnerDictionary.TryGetValue(key, out value);
            }
            else if (Properties != null)
            {
                for (var i = 0; i < Properties.Length; i++)
                {
                    var property = Properties[i];
                    if (Comparer.Equals(property.Name, key))
                    {
                        value = property.ValueGetter(Value);
                        return true;
                    }
                }

                value = null;
                return false;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private void EnsureWritable()
        {
            if (InnerDictionary == null && Properties == null)
            {
                InnerDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            else if (InnerDictionary == null)
            {
                InnerDictionary = new Dictionary<string, object>(Properties.Length, StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < Properties.Length; i++)
                {
                    var property = Properties[i];
                    InnerDictionary.Add(property.Property.Name, property.ValueGetter(Value));
                }
            }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private readonly RouteValueDictionary _dictionary;

            private int _index;
            private Dictionary<string, object>.Enumerator _enumerator;

            public Enumerator(RouteValueDictionary dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }

                _dictionary = dictionary;

                Current = default(KeyValuePair<string, object>);
                _index = -1;
                _enumerator = _dictionary.InnerDictionary == null ?
                    default(Dictionary<string, object>.Enumerator) :
                    _dictionary.InnerDictionary.GetEnumerator();
            }

            public KeyValuePair<string, object> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_dictionary?.InnerDictionary != null)
                {
                    if (_enumerator.MoveNext())
                    {
                        Current = _enumerator.Current;
                        return true;
                    }
                }
                else if (_dictionary?.Properties != null)
                {
                    _index++;
                    if (_index < _dictionary.Properties.Length)
                    {
                        var property = _dictionary.Properties[_index];
                        var value = property.ValueGetter(_dictionary.Value);
                        Current = new KeyValuePair<string, object>(property.Name, value);
                        return true;
                    }
                }

                Current = default(KeyValuePair<string, object>);
                return false;
            }

            public void Reset()
            {
                Current = default(KeyValuePair<string, object>);
                _index = -1;
                _enumerator = _dictionary?.InnerDictionary == null ?
                    default(Dictionary<string, object>.Enumerator) :
                    _dictionary.InnerDictionary.GetEnumerator();
            }
        }
    }
}
