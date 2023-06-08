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

    internal void PopPrefix(string _)
    {
        // For right now, we don't need to do anything with the prefix
        _prefix = "";
    }

    internal void PopPrefix(ReadOnlySpan<char> _)
    {
        // For right now, we don't need to do anything with the prefix
        _prefix = "";
    }

    internal void PushPrefix(string prefix)
    {
        _prefix = prefix;
    }

    internal void PushPrefix(ReadOnlySpan<char> prefix)
    {
        // For right now, just make it a string
        _prefix = prefix.ToString();
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
