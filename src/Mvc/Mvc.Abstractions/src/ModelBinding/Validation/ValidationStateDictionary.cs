// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Used for tracking validation state to customize validation behavior for a model object.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DictionaryDebugView<object, ValidationStateEntry>))]
public class ValidationStateDictionary :
    IDictionary<object, ValidationStateEntry>,
    IReadOnlyDictionary<object, ValidationStateEntry>
{
    // Created on first write (or when a live view is requested) since most binding contexts never add entries.
    private Dictionary<object, ValidationStateEntry>? _inner;

    /// <summary>
    /// Creates a new <see cref="ValidationStateDictionary"/>.
    /// </summary>
    public ValidationStateDictionary()
    {
    }

    private Dictionary<object, ValidationStateEntry> Inner =>
        _inner ??= new Dictionary<object, ValidationStateEntry>(ReferenceEqualityComparer.Instance);

    /// <inheritdoc />
    public ValidationStateEntry? this[object key]
    {
        get
        {
            TryGetValue(key, out var entry);
            return entry;
        }

        set
        {
            Inner[key] = value!;
        }
    }

    /// <inheritdoc />
    ValidationStateEntry IDictionary<object, ValidationStateEntry>.this[object key]
    {
        get => this[key]!;
        set => this[key] = value;
    }

    ValidationStateEntry IReadOnlyDictionary<object, ValidationStateEntry>.this[object key] => this[key]!;

    /// <inheritdoc />
    public int Count => _inner?.Count ?? 0;

    /// <inheritdoc />
    public bool IsReadOnly => ((IDictionary<object, ValidationStateEntry>)Inner).IsReadOnly;

    /// <inheritdoc />
    public ICollection<object> Keys => ((IDictionary<object, ValidationStateEntry>)Inner).Keys;

    /// <inheritdoc />
    public ICollection<ValidationStateEntry> Values => ((IDictionary<object, ValidationStateEntry>)Inner).Values;

    /// <inheritdoc />
    IEnumerable<object> IReadOnlyDictionary<object, ValidationStateEntry>.Keys =>
        ((IReadOnlyDictionary<object, ValidationStateEntry>)Inner).Keys;

    /// <inheritdoc />
    IEnumerable<ValidationStateEntry> IReadOnlyDictionary<object, ValidationStateEntry>.Values =>
        ((IReadOnlyDictionary<object, ValidationStateEntry>)Inner).Values;

    /// <inheritdoc />
    public void Add(KeyValuePair<object, ValidationStateEntry> item)
    {
        ((IDictionary<object, ValidationStateEntry>)Inner).Add(item);
    }

    /// <inheritdoc />
    public void Add(object key, ValidationStateEntry value)
    {
        Inner.Add(key, value);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _inner?.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<object, ValidationStateEntry> item)
    {
        return ((IDictionary<object, ValidationStateEntry>)Inner).Contains(item);
    }

    /// <inheritdoc />
    public bool ContainsKey(object key)
    {
        if (_inner is null)
        {
            ArgumentNullException.ThrowIfNull(key);
            return false;
        }

        return _inner.ContainsKey(key);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<object, ValidationStateEntry>[] array, int arrayIndex)
    {
        ((IDictionary<object, ValidationStateEntry>)Inner).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<object, ValidationStateEntry>> GetEnumerator()
    {
        return ((IDictionary<object, ValidationStateEntry>)Inner).GetEnumerator();
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<object, ValidationStateEntry> item)
    {
        return Inner.Remove(item);
    }

    /// <inheritdoc />
    public bool Remove(object key)
    {
        return Inner.Remove(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(object key, [MaybeNullWhen(false)] out ValidationStateEntry value)
    {
        if (_inner is null)
        {
            ArgumentNullException.ThrowIfNull(key);
            value = null;
            return false;
        }

        return _inner.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IDictionary<object, ValidationStateEntry>)Inner).GetEnumerator();
    }
}
