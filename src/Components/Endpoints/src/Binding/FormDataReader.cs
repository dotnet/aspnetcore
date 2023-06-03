// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal struct FormDataReader
{
    //private readonly IReadOnlyDictionary<string, StringValues> _formCollection;
    //private string _prefix;

    private readonly IReadOnlyDictionary<Prefix, StringValues> _readOnlyMemoryKeys;
    private readonly Memory<char> _prefixBuffer;
    private Memory<char> _currentPrefixBuffer;

    private IReadOnlyDictionary<Prefix, HashSet<Prefix>>? _formDictionaryKeysByPrefix;

    public FormDataReader(IReadOnlyDictionary<string, StringValues> formCollection, CultureInfo culture)
    {
        //_formCollection = formCollection;
        _readOnlyMemoryKeys = CreateReadOnlyMemoryKeys(formCollection);

        //_prefix = "";
        _prefixBuffer = ArrayPool<char>.Shared.Rent(2048);
        Culture = culture;
    }

    public FormDataReader(IReadOnlyDictionary<Prefix, StringValues> formCollection, CultureInfo culture, Memory<char> buffer)
    {
        _readOnlyMemoryKeys = formCollection;
        Culture = culture;
        _prefixBuffer = buffer;
    }

    private static IReadOnlyDictionary<Prefix, StringValues> CreateReadOnlyMemoryKeys(IReadOnlyDictionary<string, StringValues> formCollection)
    {
        var result = new Dictionary<Prefix, StringValues>(formCollection.Count);
        foreach (var key in formCollection.Keys)
        {
            result.Add(new Prefix(key.AsMemory()), formCollection[key]);
        }

        return result;
    }

    public IFormatProvider Culture { get; internal set; }

    internal IEnumerable<Prefix> GetKeys()
    {
        if (_formDictionaryKeysByPrefix == null)
        {
            _formDictionaryKeysByPrefix = ProcessFormKeys();
        }

        if (_formDictionaryKeysByPrefix.TryGetValue(new Prefix(_currentPrefixBuffer), out var foundKeys))
        {
            return foundKeys;
        }

        return Array.Empty<Prefix>();
    }

    private IReadOnlyDictionary<Prefix, HashSet<Prefix>> ProcessFormKeys()
    {
        var keys = _readOnlyMemoryKeys.Keys;
        var result = new Dictionary<Prefix, HashSet<Prefix>>();
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
                if (result.TryGetValue(new Prefix(prefix), out var foundKeys))
                {
                    foundKeys.Add(new Prefix(keyValue));
                }
                else
                {
                    result.Add(new Prefix(prefix), new HashSet<Prefix> { new Prefix(keyValue) });
                }

                startIndex = key.Value.Span[(endIndex + 1)..].IndexOf('[');
            }
        }

        return result;
    }

    internal void PopPrefix(string key)
    {
        PopPrefix(key.AsSpan());
        //var length = key.Length;
        //// If length is bigger than the current scope length typically means there is a 
        //// bug where some part of the code has not popped the scope appropriately.
        //if (_prefix.Length == length || _prefix[^(length + 1)] != '.')
        //{
        //    _prefix = _prefix[..^length];
        //}
        //else
        //{
        //    _prefix = _prefix[..^(length + 1)];
        //}
    }

    internal void PopPrefix(ReadOnlySpan<char> key)
    {
        // For right now, we don't need to do anything with the key
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

        //// We automatically append a "." before adding the suffix, except when its the first element pushed to the
        //// scope, or when we are accessing a property after a collection or an indexer like items[1].
        //var separator = _prefix.Length > 0 && _prefix[_prefix.Length - 1] != ']' && key[0] != '['
        //    ? "."
        //    : "";

        //_prefix = _prefix + separator + key;
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

        // For right now, just make it a string
        // PushPrefix(key.ToString());
    }

    internal readonly bool TryGetValue([NotNullWhen(true)] out string? value)
    {
        var foundSingleValue = _readOnlyMemoryKeys.TryGetValue(new Prefix(_currentPrefixBuffer), out var result) || result.Count == 1;
        if (foundSingleValue)
        {
            value = result[0];
        }
        else
        {
            value = null;
        }

        return foundSingleValue;
        //if (_formCollection.TryGetValue(_prefix, out var rawValue) && rawValue.Count == 1)
        //{
        //    value = rawValue[0]!;
        //    return true;
        //}
        //else
        //{
        //    value = null;
        //    return false;
        //}
    }
}

internal struct Prefix(ReadOnlyMemory<char> value) : IEquatable<Prefix>
{
    int? _hashCode;

    public ReadOnlyMemory<char> Value { get; } = value;

    public override readonly bool Equals(object? obj)
    {
        return obj is Prefix prefix &&
               Value.Equals(prefix.Value);
    }

    public readonly bool Equals(Prefix other) =>
        MemoryExtensions.Equals(Value.Span, other.Value.Span, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        _hashCode ??= string.GetHashCode(value.Span, StringComparison.OrdinalIgnoreCase);
}
