// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace BlazorUnitedApp.Resources;

internal static class StaticLocalization
{
    public static string ValidationRequired => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "de" => "Das Feld {0} ist erforderlich.",
        "fr" => "Le champ {0} est obligatoire.",
        _ => "{0} is required.",
    };

    public static string RegistrationFullName => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "de" => "Vollständiger Name",
        "fr" => "Nom complet",
        _ => "Full name",
    };
}
