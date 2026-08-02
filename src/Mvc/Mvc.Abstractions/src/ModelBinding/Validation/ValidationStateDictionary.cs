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
    private readonly Dictionary<object, ValidationStateEntry> _inner;

    /// <summary>
    /// Creates a new <see cref="ValidationStateDictionary"/>.
    /// </summary>
    public ValidationStateDictionary()
    {
        _inner = new Dictionary<object, ValidationStateEntry>(ReferenceEqualityComparer.Instance);
    }

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
            _inner[key] = value!;
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
    public int Count => _inner.Count;

    /// <inheritdoc />
    public bool IsReadOnly => ((IDictionary<object, ValidationStateEntry>)_inner).IsReadOnly;

    /// <inheritdoc />
    public ICollection<object> Keys => ((IDictionary<object, ValidationStateEntry>)_inner).Keys;

    /// <inheritdoc />
    public ICollection<ValidationStateEntry> Values => ((IDictionary<object, ValidationStateEntry>)_inner).Values;

    /// <inheritdoc />
    IEnumerable<object> IReadOnlyDictionary<object, ValidationStateEntry>.Keys =>
        ((IReadOnlyDictionary<object, ValidationStateEntry>)_inner).Keys;

    /// <inheritdoc />
    IEnumerable<ValidationStateEntry> IReadOnlyDictionary<object, ValidationStateEntry>.Values =>
        ((IReadOnlyDictionary<object, ValidationStateEntry>)_inner).Values;

    /// <inheritdoc />
    public void Add(KeyValuePair<object, ValidationStateEntry> item)
    {
        ((IDictionary<object, ValidationStateEntry>)_inner).Add(item);
    }

    /// <inheritdoc />
    public void Add(object key, ValidationStateEntry value)
    {
        _inner.Add(key, value);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _inner.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<object, ValidationStateEntry> item)
    {
        return ((IDictionary<object, ValidationStateEntry>)_inner).Contains(item);
    }

    /// <inheritdoc />
    public bool ContainsKey(object key)
    {
        return _inner.ContainsKey(key);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<object, ValidationStateEntry>[] array, int arrayIndex)
    {
        ((IDictionary<object, ValidationStateEntry>)_inner).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<object, ValidationStateEntry>> GetEnumerator()
    {
        return ((IDictionary<object, ValidationStateEntry>)_inner).GetEnumerator();
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<object, ValidationStateEntry> item)
    {
        return _inner.Remove(item);
    }

    /// <inheritdoc />
    public bool Remove(object key)
    {
        return _inner.Remove(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(object key, [MaybeNullWhen(false)] out ValidationStateEntry value)
    {
        return _inner.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IDictionary<object, ValidationStateEntry>)_inner).GetEnumerator();
    }
}
