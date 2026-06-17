// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// The HttpRequest query string collection
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(StringValuesDictionaryDebugView))]
internal sealed class QueryCollectionInternal : IQueryCollection
{
    private AdaptiveCapacityDictionary<string, StringValues> Store { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="QueryCollection"/>.
    /// </summary>
    /// <param name="store">The backing store.</param>
    internal QueryCollectionInternal(AdaptiveCapacityDictionary<string, StringValues> store)
    {
        Store = store;
    }

    /// <summary>
    /// Gets the associated set of values from the collection.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <returns>the associated value from the collection as a StringValues or StringValues.Empty if the key is not present.</returns>
    public StringValues this[string key] => TryGetValue(key, out var value) ? value : StringValues.Empty;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="QueryCollection" />;.
    /// </summary>
    /// <returns>The number of elements contained in the <see cref="QueryCollection" />.</returns>
    public int Count => Store.Count;

    /// <summary>
    /// Gets the collection of query names in this instance.
    /// </summary>
    public ICollection<string> Keys => Store.Keys;

    /// <summary>
    /// Determines whether the <see cref="QueryCollection" /> contains a specific key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>true if the <see cref="QueryCollection" /> contains a specific key; otherwise, false.</returns>
    public bool ContainsKey(string key) => Store.ContainsKey(key);

    /// <summary>
    /// Retrieves a value from the collection.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>true if the <see cref="QueryCollection" /> contains the key; otherwise, false.</returns>
    public bool TryGetValue(string key, out StringValues value) => Store.TryGetValue(key, out value);

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> object that can be used to iterate through the collection.</returns>
    public Enumerator GetEnumerator() => new Enumerator(Store.GetEnumerator());

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
    IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        => Store.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => Store.GetEnumerator();

    /// <summary>
    /// Enumerates a <see cref="QueryCollection"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
    {
        // Do NOT make this readonly, or MoveNext will not work
        private AdaptiveCapacityDictionary<string, StringValues>.Enumerator _dictionaryEnumerator;
        private readonly bool _notEmpty;

        internal Enumerator(AdaptiveCapacityDictionary<string, StringValues>.Enumerator dictionaryEnumerator)
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
        public KeyValuePair<string, StringValues> Current => _notEmpty ? _dictionaryEnumerator.Current : default;

        /// <inheritdoc />
        public void Dispose()
        {
        }

        object IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            if (_notEmpty)
            {
                ((IEnumerator)_dictionaryEnumerator).Reset();
            }
        }
    }
}
