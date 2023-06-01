// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal struct FormDataReader
{
    private readonly IReadOnlyDictionary<string, StringValues> _formCollection;
    private string _prefix;

    public FormDataReader(IReadOnlyDictionary<string, StringValues> formCollection, CultureInfo culture)
    {
        _formCollection = formCollection;
        _prefix = "";
        Culture = culture;
    }

    public IFormatProvider Culture { get; internal set; }

    internal IEnumerable<string> GetKeys()
    {
        return _formCollection.Keys;
    }

    internal void PopPrefix(string key)
    {
        var length = key.Length;
        // If length is bigger than the current scope length typically means there is a 
        // bug where some part of the code has not popped the scope appropriately.
        if (_prefix.Length == length || _prefix[^(length + 1)] != '.')
        {
            _prefix = _prefix[..^length];
        }
        else
        {
            _prefix = _prefix[..^(length + 1)];
        }
    }

    internal void PopPrefix(ReadOnlySpan<char> key)
    {
        // For right now, we don't need to do anything with the key
        PopPrefix(key.ToString());
    }

    internal void PushPrefix(string key)
    {
        // We automatically append a "." before adding the suffix, except when its the first element pushed to the
        // scope, or when we are accessing a property after a collection or an indexer like items[1].
        var separator = _prefix.Length > 0 && _prefix[_prefix.Length - 1] != ']' && key[0] != '['
            ? "."
            : "";

        _prefix = _prefix + separator + key;
    }

    internal void PushPrefix(ReadOnlySpan<char> key)
    {
        // For right now, just make it a string
        PushPrefix(key.ToString());
    }

    internal readonly bool TryGetValue([NotNullWhen(true)] out string? value)
    {
        if (_formCollection.TryGetValue(_prefix, out var rawValue) && rawValue.Count == 1)
        {
            value = rawValue[0]!;
            return true;
        }
        else
        {
            value = null;
            return false;
        }
    }
}
