// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public sealed class SimpleValueProvider : Dictionary<string, object>, IValueProvider
{
    private readonly CultureInfo _culture;

    public SimpleValueProvider()
        : this(null)
    {
    }

    public SimpleValueProvider(CultureInfo culture)
        : base(StringComparer.OrdinalIgnoreCase)
    {
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    public bool ContainsPrefix(string prefix)
    {
        foreach (string key in Keys)
        {
            if (ModelStateDictionary.StartsWithPrefix(prefix, key))
            {
                return true;
            }
        }

        return false;
    }

    public ValueProviderResult GetValue(string key)
    {
        if (TryGetValue(key, out var rawValue))
        {
            if (rawValue != null && rawValue.GetType().IsArray)
            {
                var array = (Array)rawValue;

                var stringValues = new string[array.Length];
                for (var i = 0; i < array.Length; i++)
                {
                    stringValues[i] = array.GetValue(i) as string ?? Convert.ToString(array.GetValue(i), _culture);
                }

                return new ValueProviderResult(stringValues, _culture);
            }
            else
            {
                var stringValue = rawValue as string ?? Convert.ToString(rawValue, _culture) ?? string.Empty;
                return new ValueProviderResult(stringValue, _culture);
            }
        }

        return ValueProviderResult.None;
    }
}
