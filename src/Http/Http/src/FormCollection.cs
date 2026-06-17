// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Contains the parsed HTTP form values.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(StringValuesDictionaryDebugView))]
public class FormCollection : IFormCollection
{
    /// <summary>
    /// An empty <see cref="FormCollection"/>.
    /// </summary>
    public static readonly FormCollection Empty = new FormCollection();
    private static readonly string[] EmptyKeys = Array.Empty<string>();

    // Pre-box
    private static readonly IEnumerator<KeyValuePair<string, StringValues>> EmptyIEnumeratorType = default(Enumerator);
    private static readonly IEnumerator EmptyIEnumerator = default(Enumerator);

    private static readonly IFormFileCollection EmptyFiles = new FormFileCollection();

    private IFormFileCollection? _files;

    private FormCollection()
    {
        // For static Empty
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FormCollection"/>.
    /// </summary>
    /// <param name="fields">The backing fields.</param>
    /// <param name="files">The files associated with the form.</param>
    public FormCollection(Dictionary<string, StringValues>? fields, IFormFileCollection? files = null)
    {
        // can be null
        Store = fields;
        _files = files;
    }

    /// <summary>
    /// Gets the files associated with the HTTP form.
    /// </summary>
    public IFormFileCollection Files
    {
        get => _files ?? EmptyFiles;
        private set => _files = value;
    }

    private Dictionary<string, StringValues>? Store { get; set; }

    /// <summary>
    /// Get or sets the associated value from the collection as a single string.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <returns>the associated value from the collection as a <see cref="StringValues"/>
    /// or <see cref="StringValues.Empty"/> if the key is not present.</returns>
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

    /// <inheritdoc />
    public int Count
    {
        get
        {
            return Store?.Count ?? 0;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        if (Store == null)
        {
            return false;
        }
        return Store.ContainsKey(key);
    }

    /// <inheritdoc />
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
    /// Returns an struct enumerator that iterates through a collection without boxing and
    /// is also used via the <see cref="IFormCollection" /> interface.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> object that can be used to iterate through the collection.</returns>
    public Enumerator GetEnumerator()
    {
        if (Store == null || Store.Count == 0)
        {
            // Non-boxed Enumerator
            return default;
        }
        // Non-boxed Enumerator
        return new Enumerator(Store.GetEnumerator());
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection, boxes in non-empty path.
    /// </summary>
    /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
    {
        if (Store == null || Store.Count == 0)
        {
            // Non-boxed Enumerator
            return EmptyIEnumeratorType;
        }
        // Boxed Enumerator
        return Store.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection, boxes in non-empty path.
    /// </summary>
    /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        if (Store == null || Store.Count == 0)
        {
            // Non-boxed Enumerator
            return EmptyIEnumerator;
        }
        // Boxed Enumerator
        return Store.GetEnumerator();
    }

    /// <summary>
    /// Enumerates a <see cref="FormCollection"/>.
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
        /// Advances the enumerator to the next element of the <see cref="FormCollection"/>.
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
                return default;
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
