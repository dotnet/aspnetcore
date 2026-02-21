// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace MinimalApiValidationSample;

/// <summary>
/// An <see cref="IStringLocalizerFactory"/> backed by in-memory dictionaries for
/// English, Spanish, and French. Used to demonstrate validation localization without
/// requiring resource files.
/// </summary>
public sealed class HardcodedStringLocalizerFactory : IStringLocalizerFactory
{
    private static readonly HardcodedStringLocalizer SharedLocalizer = new();

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource) => SharedLocalizer;

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location) => SharedLocalizer;
}

/// <summary>
/// An <see cref="IStringLocalizer"/> that resolves validation error messages and display
/// names from hardcoded dictionaries. Supports English (en), Spanish (es), and French (fr).
/// </summary>
public sealed class HardcodedStringLocalizer : IStringLocalizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> LanguageData = new()
    {
        ["en"] = new()
        {
            ["RequiredError"] = "The {0} field is required.",
            ["LengthError"] = "The {0} must be between {2} and {1} characters.",
            ["EmailInvalid"] = "Please enter a valid email address.",
            ["PhoneInvalid"] = "Please enter a valid phone number.",
            ["RangeError"] = "The {0} must be between {1} and {2}.",
            ["Name"] = "Name",
            ["Email"] = "Email",
            ["Phone"] = "Phone",
            ["Message"] = "Message",
            ["Age"] = "Age",
        },
        ["es"] = new()
        {
            ["RequiredError"] = "El campo {0} es obligatorio.",
            ["LengthError"] = "El {0} debe tener entre {2} y {1} caracteres.",
            ["EmailInvalid"] = "Por favor ingrese una dirección de correo válida.",
            ["PhoneInvalid"] = "Por favor ingrese un número de teléfono válido.",
            ["RangeError"] = "El {0} debe estar entre {1} y {2}.",
            ["Name"] = "Nombre",
            ["Email"] = "Correo electrónico",
            ["Phone"] = "Teléfono",
            ["Message"] = "Mensaje",
            ["Age"] = "Edad",
        },
        ["fr"] = new()
        {
            ["RequiredError"] = "Le champ {0} est obligatoire.",
            ["LengthError"] = "Le {0} doit contenir entre {2} et {1} caractères.",
            ["EmailInvalid"] = "Veuillez entrer une adresse email valide.",
            ["PhoneInvalid"] = "Veuillez entrer un numéro de téléphone valide.",
            ["RangeError"] = "Le {0} doit être compris entre {1} et {2}.",
            ["Name"] = "Nom",
            ["Email"] = "E-mail",
            ["Phone"] = "Téléphone",
            ["Message"] = "Message",
            ["Age"] = "Âge",
        },
    };

    /// <inheritdoc />
    public LocalizedString this[string name] => GetLocalizedString(name, arguments: null);

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var strings = GetLanguageStrings();
        return strings.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false));
    }

    private static Dictionary<string, string> GetLanguageStrings()
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return LanguageData.TryGetValue(language, out var strings) ? strings : LanguageData["en"];
    }

    private static LocalizedString GetLocalizedString(string name, object[]? arguments)
    {
        var languageStrings = GetLanguageStrings();

        if (!languageStrings.TryGetValue(name, out var value))
        {
            value = name;
        }

        var formattedValue = arguments is { Length: > 0 }
            ? string.Format(CultureInfo.CurrentCulture, value, arguments)
            : value;

        return new LocalizedString(name, formattedValue, resourceNotFound: value == name);
    }
}
