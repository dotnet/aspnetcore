// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace BlazorNet11.Localization;

public class JsonStringLocalizer : IStringLocalizer
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations;

    public JsonStringLocalizer(Dictionary<string, Dictionary<string, string>> translations)
    {
        _translations = translations;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (_translations.TryGetValue(culture, out var strings) && strings.TryGetValue(name, out var value))
            {
                return new LocalizedString(name, value, resourceNotFound: false);
            }

            if (_translations.TryGetValue("en", out var fallback) && fallback.TryGetValue(name, out var fbValue))
            {
                return new LocalizedString(name, fbValue, resourceNotFound: false);
            }

            return new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var str = this[name];
            if (str.ResourceNotFound)
            {
                return str;
            }

            return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, str.Value, arguments), resourceNotFound: false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (_translations.TryGetValue(culture, out var strings))
        {
            return strings.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, false));
        }

        return [];
    }
}
