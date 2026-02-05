// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace MinimalValidationSample;

public class HardcodedStringLocalizer : IStringLocalizer
{
    public static HardcodedStringLocalizer Instance { get; } = new();

    private static readonly Dictionary<string, Dictionary<string, string>> LanguageData = new()
    {
        {
            "en", new Dictionary<string, string>
            {
                { "RequiredError", "The {0} field is required." },
                { "LengthError", "The {0} must be between {2} and {1} characters." },
                { "EmailInvalid", "Please enter a valid email address." },
                { "PhoneInvalid", "Please enter a valid phone number." },
                { "RangeError", "The {0} must be between {1} and {2}." },
                { "Name", "Name" },
                { "Email", "Email" },
                { "Phone", "Phone" },
                { "Message", "Message" },
                { "Age", "Age" }
            }
        },
        {
            "es", new Dictionary<string, string>
            {
                { "RequiredError", "El campo {0} es obligatorio." },
                { "LengthError", "El {0} debe tener entre {2} y {1} caracteres." },
                { "EmailInvalid", "Por favor ingrese una dirección de correo válida." },
                { "PhoneInvalid", "Por favor ingrese un número de teléfono válido." },
                { "RangeError", "El {0} debe estar entre {1} y {2}." },
                { "Name", "Nombre" },
                { "Email", "Correo electrónico" },
                { "Phone", "Teléfono" },
                { "Message", "Mensaje" },
                { "Age", "Edad" }
            }
        },
        {
            "fr", new Dictionary<string, string>
            {
                { "RequiredError", "Le champ {0} est obligatoire." },
                { "LengthError", "Le {0} doit contenir entre {2} et {1} caractères." },
                { "EmailInvalid", "Veuillez entrer une adresse email valide." },
                { "PhoneInvalid", "Veuillez entrer un numéro de téléphone valide." },
                { "RangeError", "Le {0} doit être compris entre {1} et {2}." },
                { "Name", "Nom" },
                { "Email", "E-mail" },
                { "Phone", "Téléphone" },
                { "Message", "Message" },
                { "Age", "Âge" }
            }
        }
    };

    public LocalizedString this[string name] => GetLocalizedString(name, arguments: null);

    public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (!LanguageData.TryGetValue(language, out var languageStrings))
        {
            languageStrings = LanguageData["en"];
        }

        return languageStrings
            .Select(kvp => new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false));
    }

    private static LocalizedString GetLocalizedString(string name, object[]? arguments)
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (!LanguageData.TryGetValue(language, out var languageStrings))
        {
            languageStrings = LanguageData["en"];
        }

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
