// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

internal sealed class ElementalValueProvider : IValueProvider
{
    public ElementalValueProvider(string key, string? value, CultureInfo culture)
    {
        Key = key;
        Value = value;
        Culture = culture;
    }

    public CultureInfo Culture { get; }

    public string Key { get; }

    public string? Value { get; }

    public bool ContainsPrefix(string prefix)
    {
        return ModelStateDictionary.StartsWithPrefix(prefix, Key);
    }

    public ValueProviderResult GetValue(string key)
    {
        if (string.Equals(key, Key, StringComparison.OrdinalIgnoreCase))
        {
            return new ValueProviderResult(Value, Culture);
        }
        else
        {
            return ValueProviderResult.None;
        }
    }
}
