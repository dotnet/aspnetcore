// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents a wrapper for RequestHeaders and ResponseHeaders.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(StringValuesDictionaryDebugView))]
public class HeaderDictionary : IHeaderDictionary
{
    private static readonly string[] EmptyKeys = Array.Empty<string>();
    private static readonly StringValues[] EmptyValues = Array.Empty<StringValues>();
    // Pre-box
    private static readonly IEnumerator<KeyValuePair<string, StringValues>> EmptyIEnumeratorType = default(Enumerator);
    private static readonly IEnumerator EmptyIEnumerator = default(Enumerator);

    /// <summary>
    /// Initializes a new instance of <see cref="HeaderDictionary"/>.
    /// </summary>
    public HeaderDictionary()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HeaderDictionary"/>.
    /// </summary>
    /// <param name="store">The value to use as the backing store.</param>
    public HeaderDictionary(Dictionary<string, StringValues>? store)
    {
        Store = store;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HeaderDictionary"/>.
    /// </summary>
    /// <param name="capacity">The initial number of headers that this instance can contain.</param>
    public HeaderDictionary(int capacity)
    {
        EnsureStore(capacity);
    }

    private Dictionary<string, StringValues>? Store { get; set; }

    [MemberNotNull(nameof(Store))]
    private void EnsureStore(int capacity)
    {
        if (Store == null)
        {
            Store = new Dictionary<string, StringValues>(capacity, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Get or sets the associated value from the collection as a single string.
    /// </summary>
    /// <param name="key">The header name.</param>
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
        set
        {
            ArgumentNullException.ThrowIfNull(key);
            ThrowIfReadOnly();

            if (value.Count == 0)
            {
                Store?.Remove(key);
            }
            else
            {
                EnsureStore(1);
                Store[key] = value;
            }
        }
    }

    StringValues IDictionary<string, StringValues>.this[string key]
    {
        get
        {
            if (Store == null)
            {
                ThrowKeyNotFoundException();
            }

            return Store[key];
        }
        set
        {
            ThrowIfReadOnly();
            this[key] = value;
        }
    }

    /// <inheritdoc />
    public long? ContentLength
    {
        get
        {
            long value;
            var rawValue = this[HeaderNames.ContentLength];
            if (rawValue.Count == 1 &&
                !string.IsNullOrEmpty(rawValue[0]) &&
                HeaderUtilities.TryParseNonNegativeInt64(new StringSegment(rawValue[0]).Trim(), out value))
            {
                return value;
            }

            return null;
        }
        set
        {
            ThrowIfReadOnly();
            if (value.HasValue)
            {
                this[HeaderNames.ContentLength] = HeaderUtilities.FormatNonNegativeInt64(value.GetValueOrDefault());
            }
            else
            {
                this.Remove(HeaderNames.ContentLength);
            }
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="HeaderDictionary" />;.
    /// </summary>
    /// <returns>The number of elements contained in the <see cref="HeaderDictionary" />.</returns>
    public int Count => Store?.Count ?? 0;

    /// <summary>
    /// Gets a value that indicates whether the <see cref="HeaderDictionary" /> is in read-only mode.
    /// </summary>
    /// <returns>true if the <see cref="HeaderDictionary" /> is in read-only mode; otherwise, false.</returns>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets the collection of HTTP header names in this instance.
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
    /// Gets the collection of HTTP header values in this instance.
    /// </summary>
    public ICollection<StringValues> Values
    {
        get
        {
            if (Store == null)
            {
                return EmptyValues;
            }
            return Store.Values;
        }
    }

    /// <summary>
    /// Adds a new header item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(KeyValuePair<string, StringValues> item)
    {
        if (item.Key == null)
        {
            throw new ArgumentException("The key is null");
        }
        ThrowIfReadOnly();
        EnsureStore(1);
        Store.Add(item.Key, item.Value);
    }

    /// <summary>
    /// Adds the given header and values to the collection.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header values.</param>
    public void Add(string key, StringValues value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfReadOnly();
        EnsureStore(1);
        Store.Add(key, value);
    }

    /// <summary>
    /// Clears the entire list of objects.
    /// </summary>
    public void Clear()
    {
        ThrowIfReadOnly();
        Store?.Clear();
    }

    /// <summary>
    /// Returns a value indicating whether the specified object occurs within this collection.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>true if the specified object occurs within this collection; otherwise, false.</returns>
    public bool Contains(KeyValuePair<string, StringValues> item)
    {
        if (Store == null ||
            !Store.TryGetValue(item.Key, out var value) ||
            !StringValues.Equals(value, item.Value))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Determines whether the <see cref="HeaderDictionary" /> contains a specific key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>true if the <see cref="HeaderDictionary" /> contains a specific key; otherwise, false.</returns>
    public bool ContainsKey(string key)
    {
        if (Store == null)
        {
            return false;
        }
        return Store.ContainsKey(key);
    }

    /// <summary>
    /// Copies the <see cref="HeaderDictionary" /> elements to a one-dimensional Array instance at the specified index.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the specified objects copied from the <see cref="HeaderDictionary" />.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        if (Store == null)
        {
            return;
        }

        foreach (var item in Store)
        {
            array[arrayIndex] = item;
            arrayIndex++;
        }
    }

    /// <summary>
    /// Removes the given item from the the collection.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        ThrowIfReadOnly();
        if (Store == null)
        {
            return false;
        }

        if (Store.TryGetValue(item.Key, out var value) && StringValues.Equals(item.Value, value))
        {
            return Store.Remove(item.Key);
        }
        return false;
    }

    /// <summary>
    /// Removes the given header from the collection.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
    public bool Remove(string key)
    {
        ThrowIfReadOnly();
        if (Store == null)
        {
            return false;
        }
        return Store.Remove(key);
    }

    /// <summary>
    /// Retrieves a value from the dictionary.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The value.</param>
    /// <returns>true if the <see cref="HeaderDictionary" /> contains the key; otherwise, false.</returns>
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
    /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
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

    private void ThrowIfReadOnly()
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("The response headers cannot be modified because the response has already started.");
        }
    }

    [DoesNotReturn]
    private static void ThrowKeyNotFoundException()
    {
        throw new KeyNotFoundException();
    }

    internal string DebuggerToString()
    {
        var debugText = $"Count = {Count}";
        if (IsReadOnly)
        {
            debugText += ", IsReadOnly = true";
        }
        return debugText;
    }

    /// <summary>
    /// Enumerates a <see cref="HeaderDictionary"/>.
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
