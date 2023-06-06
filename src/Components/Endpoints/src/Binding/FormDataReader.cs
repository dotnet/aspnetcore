// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal struct FormDataReader
{
    private readonly IReadOnlyDictionary<FormKey, StringValues> _readOnlyMemoryKeys;
    private readonly Memory<char> _prefixBuffer;
    private Memory<char> _currentPrefixBuffer;

    // As an implementation detail, reuse FormKey for the values.
    // It's just a thin wrapper over ReadOnlyMemory<char> that caches
    // the computed hash code.
    private IReadOnlyDictionary<FormKey, HashSet<FormKey>>? _formDictionaryKeysByPrefix;

    public FormDataReader(IReadOnlyDictionary<FormKey, StringValues> formCollection, CultureInfo culture, Memory<char> buffer)
    {
        _readOnlyMemoryKeys = formCollection;
        Culture = culture;
        _prefixBuffer = buffer;
    }

    internal ReadOnlyMemory<char> CurrentPrefix => _currentPrefixBuffer;

    public IFormatProvider Culture { get; internal set; }

    internal FormKeyCollection GetKeys()
    {
        if (_formDictionaryKeysByPrefix == null)
        {
            _formDictionaryKeysByPrefix = ProcessFormKeys();
        }

        if (_formDictionaryKeysByPrefix.TryGetValue(new FormKey(_currentPrefixBuffer), out var foundKeys))
        {
            return new FormKeyCollection(foundKeys);
        }

        return FormKeyCollection.Empty;
    }

    // Internal for testing purposes
    internal IReadOnlyDictionary<FormKey, HashSet<FormKey>> ProcessFormKeys()
    {
        var keys = _readOnlyMemoryKeys.Keys;
        var result = new Dictionary<FormKey, HashSet<FormKey>>();
        // We need to iterate over all the keys in the dictionary and process each key to split it into segments where
        // the prefixes are string separated by . and the keys are enclosed in []. For example if the key is
        // Customer.Orders[<<OrderId>>]BillingInfo.FirstName, then we need to split it into Customer.Orders,
        // [<<OrderId>>] and BillingInfo.FirstName. We then, need to group all the keys by the prefix. So, for the
        // above example, we will have an entry for the prefix Customer.Orders that will include [<<OrderId>>] as the
        // key.

        foreach (var key in keys)
        {
            var startIndex = key.Value.Span.IndexOf('[');
            while (startIndex >= 0)
            {
                var endIndex = key.Value.Span[startIndex..].IndexOf(']') + startIndex;
                if (endIndex == -1)
                {
                    // Ignore malformed keys
                    break;
                }

                var prefix = key.Value[..startIndex];
                var keyValue = key.Value[startIndex..(endIndex + 1)];
                if (result.TryGetValue(new FormKey(prefix), out var foundKeys))
                {
                    foundKeys.Add(new FormKey(keyValue));
                }
                else
                {
                    result.Add(new FormKey(prefix), new HashSet<FormKey> { new FormKey(keyValue) });
                }

                var nextOpenBracket = key.Value.Span[(endIndex + 1)..].IndexOf('[');

                startIndex = nextOpenBracket != -1 ? endIndex + 1 + nextOpenBracket : -1;
            }
        }

        return result;
    }

    internal void PopPrefix(string key)
    {
        PopPrefix(key.AsSpan());
    }

    internal void PopPrefix(ReadOnlySpan<char> key)
    {
        var keyLength = key.Length;
        // If keyLength is bigger than the current scope keyLength typically means there is a 
        // bug where some part of the code has not popped the scope appropriately.
        Debug.Assert(_currentPrefixBuffer.Length >= keyLength);
        if (_currentPrefixBuffer.Length == keyLength || _currentPrefixBuffer.Span[^(keyLength + 1)] != '.')
        {
            _currentPrefixBuffer = _currentPrefixBuffer[..^keyLength];
        }
        else
        {
            _currentPrefixBuffer = _currentPrefixBuffer[..^(keyLength + 1)];
        }
    }

    internal void PushPrefix(string key)
    {
        PushPrefix(key.AsSpan());
    }

    internal void PushPrefix(scoped ReadOnlySpan<char> key)
    {
        // We automatically append a "." before adding the suffix, except when its the first element pushed to the
        // scope, or when we are accessing a property after a collection or an indexer like items[1].
        var separator = _currentPrefixBuffer.Length > 0 && key[0] != '['
            ? ".".AsSpan()
            : "".AsSpan();

        Debug.Assert(_prefixBuffer.Length >= (_currentPrefixBuffer.Length + separator.Length));

        separator.CopyTo(_prefixBuffer.Span[_currentPrefixBuffer.Length..]);

        var startingPoint = _currentPrefixBuffer.Length + separator.Length;
        _currentPrefixBuffer = _prefixBuffer[..(_currentPrefixBuffer.Length + separator.Length + key.Length)];
        key.CopyTo(_prefixBuffer[startingPoint..].Span);
    }

    internal readonly bool TryGetValue([NotNullWhen(true)] out string? value)
    {
        var foundSingleValue = _readOnlyMemoryKeys.TryGetValue(new FormKey(_currentPrefixBuffer), out var result) || result.Count == 1;
        if (foundSingleValue)
        {
            value = result[0];
        }
        else
        {
            value = null;
        }

        return foundSingleValue;
    }

    internal readonly struct FormKeyCollection : IEnumerable<ReadOnlyMemory<char>>
    {
        private readonly HashSet<FormKey> _values;
        internal static readonly FormKeyCollection Empty;

        public bool HasValues() => _values != null;

        public FormKeyCollection(HashSet<FormKey> values) => _values = values;

        public Enumerator GetEnumerator() => new Enumerator(_values.GetEnumerator());

        IEnumerator<ReadOnlyMemory<char>> IEnumerable<ReadOnlyMemory<char>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal struct Enumerator : IEnumerator<ReadOnlyMemory<char>>
        {
            private HashSet<FormKey>.Enumerator _enumerator;

            public Enumerator(HashSet<FormKey>.Enumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public ReadOnlyMemory<char> Current => _enumerator.Current.Value;

            object IEnumerator.Current => _enumerator.Current;

            void IDisposable.Dispose() => _enumerator.Dispose();

            public bool MoveNext() => _enumerator.MoveNext();

            void IEnumerator.Reset() { }
        }
    }
}
