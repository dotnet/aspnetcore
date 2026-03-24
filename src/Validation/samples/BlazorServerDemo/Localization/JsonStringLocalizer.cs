// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace BlazorServerDemo.Localization;

/// <summary>
/// An <see cref="IStringLocalizer"/> that reads translations from a JSON file.
/// </summary>
public sealed class JsonStringLocalizer : IStringLocalizer
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _translations;

    public JsonStringLocalizer(ConcurrentDictionary<string, Dictionary<string, string>> translations)
    {
        _translations = translations;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = GetString(name);
            var value = format is not null
                ? string.Format(CultureInfo.CurrentCulture, format, arguments)
                : name;
            return new LocalizedString(name, value, resourceNotFound: format is null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (_translations.TryGetValue(culture, out var strings))
        {
            foreach (var kvp in strings)
            {
                yield return new LocalizedString(kvp.Key, kvp.Value);
            }
        }
    }

    private string? GetString(string name)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (_translations.TryGetValue(culture, out var cultureStrings) &&
            cultureStrings.TryGetValue(name, out var value))
        {
            return value;
        }

        // Fall back to English
        if (culture != "en" &&
            _translations.TryGetValue("en", out var fallbackStrings) &&
            fallbackStrings.TryGetValue(name, out var fallbackValue))
        {
            return fallbackValue;
        }

        return null;
    }
}
