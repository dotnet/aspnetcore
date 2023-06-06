// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    private IReadOnlyDictionary<FormKey, HashSet<FormKey>>? _formDictionaryKeysByPrefix;

    public FormDataReader(IReadOnlyDictionary<FormKey, StringValues> formCollection, CultureInfo culture, Memory<char> buffer)
    {
        _readOnlyMemoryKeys = formCollection;
        Culture = culture;
        _prefixBuffer = buffer;
    }

    public IFormatProvider Culture { get; internal set; }

    internal IEnumerable<FormKey> GetKeys()
    {
        if (_formDictionaryKeysByPrefix == null)
        {
            _formDictionaryKeysByPrefix = ProcessFormKeys();
        }

        if (_formDictionaryKeysByPrefix.TryGetValue(new FormKey(_currentPrefixBuffer), out var foundKeys))
        {
            return foundKeys;
        }

        return Array.Empty<FormKey>();
    }

    private IReadOnlyDictionary<FormKey, HashSet<FormKey>> ProcessFormKeys()
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
                var endIndex = key.Value.Span.IndexOf(']');
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

                startIndex = key.Value.Span[(endIndex + 1)..].IndexOf('[');
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
        var length = key.Length;
        // If length is bigger than the current scope length typically means there is a 
        // bug where some part of the code has not popped the scope appropriately.
        Debug.Assert(_currentPrefixBuffer.Length >= length);
        if (_currentPrefixBuffer.Length == length || _currentPrefixBuffer.Span[^(length + 1)] != '.')
        {
            _currentPrefixBuffer = _currentPrefixBuffer[..^length];
        }
        else
        {
            _currentPrefixBuffer = _currentPrefixBuffer[..^(length + 1)];
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
        var separator = _currentPrefixBuffer.Length > 0 && _currentPrefixBuffer.Span[_currentPrefixBuffer.Length - 1] != ']' && key[0] != '['
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
}
