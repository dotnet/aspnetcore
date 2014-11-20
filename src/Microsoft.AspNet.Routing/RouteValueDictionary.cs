// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Routing
{
    /// <summary>
    /// An <see cref="IDictionary{string, object}"/> type for route values.
    /// </summary>
    public class RouteValueDictionary : IDictionary<string, object>, IReadOnlyDictionary<string, object>
    {
        /// <summary>
        /// An empty, cached instance of <see cref="RouteValueDictionary"/>.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, object> Empty = new RouteValueDictionary();

        private readonly Dictionary<string, object> _dictionary;

        /// <summary>
        /// Creates an empty RouteValueDictionary.
        /// </summary>
        public RouteValueDictionary()
        {
            _dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a RouteValueDictionary initialized with the provided input values.
        /// </summary>
        /// <param name="values">Input values to copy into the dictionary.</param>
        public RouteValueDictionary([NotNull] IDictionary<string, object> values)
        {
            _dictionary = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a RouteValueDictionary initialized with the provided input values.
        /// </summary>
        /// <param name="values">Input values to copy into the dictionary.</param>
        /// <remarks>
        /// The input parameter is interpreted as a set of key-value-pairs where the property names
        /// are keys, and property values are the values, and copied into the dictionary. Only public
        /// instance non-index properties are considered.
        /// </remarks>
        public RouteValueDictionary(object obj)
            : this()
        {
            if (obj != null)
            {
                var type = obj.GetType();
                var allProperties = type.GetRuntimeProperties();

                // This is done to support 'new' properties that hide a property on a base class
                var orderedByDeclaringType = allProperties.OrderBy(p => p.DeclaringType == type ? 0 : 1);
                foreach (var property in orderedByDeclaringType)
                {
                    if (property.GetMethod != null &&
                        property.GetMethod.IsPublic &&
                        !property.GetMethod.IsStatic &&
                        property.GetIndexParameters().Length == 0)
                    {
                        var value = property.GetValue(obj);
                        if (ContainsKey(property.Name) && property.DeclaringType != type)
                        {
                            // This is a hidden property, ignore it.
                        }
                        else
                        {
                            Add(property.Name, value);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public object this[[NotNull] string key]
        {
            get
            {
                object value;
                _dictionary.TryGetValue(key, out value);
                return value;
            }

            set
            {
                _dictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets the comparer for this dictionary.
        /// </summary>
        /// <remarks>
        /// This will always be a reference to <see cref="StringComparer.OrdinalIgnoreCase"/>
        /// </remarks>
        public IEqualityComparer<string> Comparer
        {
            get
            {
                return _dictionary.Comparer;
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get
            {
                return ((ICollection<KeyValuePair<string, object>>)_dictionary).IsReadOnly;
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object>.KeyCollection Keys
        {
            get
            {
                return _dictionary.Keys;
            }
        }

        /// <inheritdoc />
        ICollection<string> IDictionary<string, object>.Keys
        {
            get
            {
                return _dictionary.Keys;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys
        {
            get
            {
                return _dictionary.Keys;
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object>.ValueCollection Values
        {
            get
            {
                return _dictionary.Values;
            }
        }

        /// <inheritdoc />
        ICollection<object> IDictionary<string, object>.Values
        {
            get
            {
                return _dictionary.Values;
            }
        }

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values
        {
            get
            {
                return _dictionary.Values;
            }
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)_dictionary).Add(item);
        }

        /// <inheritdoc />
        public void Add([NotNull] string key, object value)
        {
            _dictionary.Add(key, value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_dictionary).Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey([NotNull] string key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, object>>.CopyTo(
            [NotNull] KeyValuePair<string, object>[] array,
            int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)_dictionary).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public Dictionary<string, object>.Enumerator GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_dictionary).Remove(item);
        }

        /// <inheritdoc />
        public bool Remove([NotNull] string key)
        {
            return _dictionary.Remove(key);
        }

        /// <inheritdoc />
        public bool TryGetValue([NotNull] string key, out object value)
        {
            return _dictionary.TryGetValue(key, out value);
        }
    }
}
