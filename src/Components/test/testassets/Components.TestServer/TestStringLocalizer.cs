// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace TestServer;

/// <summary>
/// Test <see cref="IStringLocalizerFactory"/> backed by an in-memory dictionary of per-culture
/// translations. Returns the same <see cref="IStringLocalizer"/> instance regardless of the
/// requested resource source; culture awareness lives on the localizer (which reads
/// <see cref="CultureInfo.CurrentUICulture"/> at lookup time).
/// </summary>
internal sealed class TestStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly TestStringLocalizer _localizer;

    public TestStringLocalizerFactory(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> translations)
    {
        _localizer = new TestStringLocalizer(translations);
    }

    public IStringLocalizer Create(Type resourceSource) => _localizer;

    public IStringLocalizer Create(string baseName, string location) => _localizer;
}

/// <summary>
/// Test <see cref="IStringLocalizer"/> backed by an in-memory <c>culture → key → value</c>
/// dictionary. Reads <see cref="CultureInfo.CurrentUICulture"/> at each indexer access, so the
/// same instance can serve different cultures across requests.
/// </summary>
internal sealed class TestStringLocalizer : IStringLocalizer
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _translations;

    public TestStringLocalizer(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> translations)
    {
        _translations = translations;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return _translations.TryGetValue(lang, out var dict) && dict.TryGetValue(name, out var value)
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localized = this[name];
            return localized.ResourceNotFound
                ? localized
                : new LocalizedString(
                    name,
                    string.Format(CultureInfo.CurrentCulture, localized.Value, arguments),
                    resourceNotFound: false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Array.Empty<LocalizedString>();
}

/// <summary>
/// In-memory translation tables for the localized client-validation E2E tests. Keys cover the
/// display names and error-message keys used by the LocalizedValidation test page.
/// </summary>
internal static class ClientValidationLocalizationData
{
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Translations { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["fr"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Email Address"] = "Adresse e-mail",
                ["Age"] = "Âge",
                ["RequiredKey"] = "Le champ {0} est requis (fr)",
                ["RangeKey"] = "Le champ {0} doit être entre {1} et {2} (fr)",
            },
            ["de"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Email Address"] = "E-Mail-Adresse",
                ["Age"] = "Alter",
                ["RequiredKey"] = "Das Feld {0} ist erforderlich (de)",
                ["RangeKey"] = "Das Feld {0} muss zwischen {1} und {2} liegen (de)",
            },
        };
}
