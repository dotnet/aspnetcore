// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// The HttpRequest query string collection
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(StringValuesDictionaryDebugView))]
public class QueryCollection : IQueryCollection
{
    /// <summary>
    /// Gets an empty <see cref="QueryCollection"/>.
    /// </summary>
    public static readonly QueryCollection Empty = new QueryCollection();
    private static readonly string[] EmptyKeys = Array.Empty<string>();
    // Pre-box
    private static readonly IEnumerator<KeyValuePair<string, StringValues>> EmptyIEnumeratorType = default(Enumerator);
    private static readonly IEnumerator EmptyIEnumerator = default(Enumerator);

    private Dictionary<string, StringValues>? Store { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="QueryCollection"/>.
    /// </summary>
    public QueryCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="QueryCollection"/>.
    /// </summary>
    /// <param name="store">The backing store.</param>
    public QueryCollection(Dictionary<string, StringValues> store)
    {
        Store = store;
    }

    /// <summary>
    /// Creates a shallow copy of the specified <paramref name="store"/>.
    /// </summary>
    /// <param name="store">The <see cref="QueryCollection"/> to clone.</param>
    public QueryCollection(QueryCollection store)
    {
        Store = store.Store;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="QueryCollection"/>.
    /// </summary>
    /// <param name="capacity">The initial number of query items that this instance can contain.</param>
    public QueryCollection(int capacity)
    {
        Store = new Dictionary<string, StringValues>(capacity, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the associated set of values from the collection.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <returns>the associated value from the collection as a StringValues or StringValues.Empty if the key is not present.</returns>
    public StringValues this[string key]
    {
        get
        {
            if (Store == null)
            {
                return StringValues.Empty;
            }

            if (TryGetValue(key, out var value))
            {
                return value;
            }
            return StringValues.Empty;
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="QueryCollection" />;.
    /// </summary>
    /// <returns>The number of elements contained in the <see cref="QueryCollection" />.</returns>
    public int Count
    {
        get
        {
            if (Store == null)
            {
                return 0;
            }
            return Store.Count;
        }
    }

    /// <summary>
    /// Gets the collection of query names in this instance.
    /// </summary>
    public ICollection<string> Keys
    {
        get
        {
            if (Store == null)
            {
                return EmptyKeys;
            }
            return Store.Keys;
        }
    }

    /// <summary>
    /// Determines whether the <see cref="QueryCollection" /> contains a specific key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>true if the <see cref="QueryCollection" /> contains a specific key; otherwise, false.</returns>
    public bool ContainsKey(string key)
    {
        if (Store == null)
        {
            return false;
        }
        return Store.ContainsKey(key);
    }

    /// <summary>
    /// Retrieves a value from the collection.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>true if the <see cref="QueryCollection" /> contains the key; otherwise, false.</returns>
    public bool TryGetValue(string key, out StringValues value)
    {
        if (Store == null)
        {
            value = default(StringValues);
            return false;
        }
        return Store.TryGetValue(key, out value);
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> object that can be used to iterate through the collection.</returns>
    public Enumerator GetEnumerator()
    {
        if (Store == null || Store.Count == 0)
        {
            // Non-boxed Enumerator
            return default;
        }
        return new Enumerator(Store.GetEnumerator());
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
    IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
    {
        if (Store == null || Store.Count == 0)
        {
            // Non-boxed Enumerator
            return EmptyIEnumeratorType;
        }
        return Store.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        if (Store == null || Store.Count == 0)
        {
            // Non-boxed Enumerator
            return EmptyIEnumerator;
        }
        return Store.GetEnumerator();
    }

    /// <summary>
    /// Enumerates a <see cref="QueryCollection"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
    {
        // Do NOT make this readonly, or MoveNext will not work
        private Dictionary<string, StringValues>.Enumerator _dictionaryEnumerator;
        private readonly bool _notEmpty;

        internal Enumerator(Dictionary<string, StringValues>.Enumerator dictionaryEnumerator)
        {
            _dictionaryEnumerator = dictionaryEnumerator;
            _notEmpty = true;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the <see cref="HeaderDictionary"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element;
        /// <see langword="false"/> if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            if (_notEmpty)
            {
                return _dictionaryEnumerator.MoveNext();
            }
            return false;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public KeyValuePair<string, StringValues> Current
        {
            get
            {
                if (_notEmpty)
                {
                    return _dictionaryEnumerator.Current;
                }
                return default(KeyValuePair<string, StringValues>);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        void IEnumerator.Reset()
        {
            if (_notEmpty)
            {
                ((IEnumerator)_dictionaryEnumerator).Reset();
            }
        }
    }
}
