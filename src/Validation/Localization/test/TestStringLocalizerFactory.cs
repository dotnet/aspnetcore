// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.Validation.Localization.Tests;

/// <summary>
/// Per-resource-source <see cref="IStringLocalizerFactory"/> backed by an in-memory dictionary.
/// Useful for verifying both the default per-type lookup pattern and the shared-resource pattern.
/// </summary>
internal sealed class TestStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly Dictionary<Type, Dictionary<string, string>> _perTypeTranslations;
    private readonly Dictionary<string, string> _defaultTranslations;

    /// <summary>
    /// Creates a factory that returns the same translation dictionary for every type.
    /// Useful when tests don't care about per-type isolation.
    /// </summary>
    public TestStringLocalizerFactory(Dictionary<string, string> translations)
    {
        _defaultTranslations = translations;
        _perTypeTranslations = [];
    }

    /// <summary>
    /// Creates a factory that returns different translation dictionaries per resource type.
    /// Types not present in <paramref name="perTypeTranslations"/> use an empty dictionary.
    /// </summary>
    public TestStringLocalizerFactory(Dictionary<Type, Dictionary<string, string>> perTypeTranslations)
    {
        _defaultTranslations = [];
        _perTypeTranslations = perTypeTranslations;
    }

    public IStringLocalizer Create(Type resourceSource)
        => new TestStringLocalizer(_perTypeTranslations.TryGetValue(resourceSource, out var t)
            ? t
            : _defaultTranslations);

    public IStringLocalizer Create(string baseName, string location)
        => new TestStringLocalizer(_defaultTranslations);
}

internal sealed class TestStringLocalizer(Dictionary<string, string> translations) : IStringLocalizer
{
    public LocalizedString this[string name] => translations.TryGetValue(name, out var value)
        ? new LocalizedString(name, value, resourceNotFound: false)
        : new LocalizedString(name, name, resourceNotFound: true);

    public LocalizedString this[string name, params object[] arguments] => translations.TryGetValue(name, out var value)
        ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false)
        : new LocalizedString(name, name, resourceNotFound: true);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        translations.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false));
}
