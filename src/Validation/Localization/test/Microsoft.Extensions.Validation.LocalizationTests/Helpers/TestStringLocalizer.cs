// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.Validation.LocalizationTests.Helpers;

internal class TestStringLocalizer(Dictionary<string, string> translations) : IStringLocalizer
{
    private readonly Dictionary<string, string> _translations = translations;

    public LocalizedString this[string name] => _translations.TryGetValue(name, out var value)
        ? new LocalizedString(name, value, resourceNotFound: false)
        : new LocalizedString(name, name, resourceNotFound: true);

    public LocalizedString this[string name, params object[] arguments] => _translations.TryGetValue(name, out var value)
        ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false)
        : new LocalizedString(name, name, resourceNotFound: true);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _translations.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, false));
}
