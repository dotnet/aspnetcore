// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Resources;

namespace BlazorNet11.Resources;

public class LocalizedStrings
{
    private static readonly ResourceManager _resourceManager =
        new ResourceManager(typeof(LocalizedStrings));

    public static string ContactEmail =>
        _resourceManager.GetString(nameof(ContactEmail), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ContactEmail);

    public static string RequiredError =>
        _resourceManager.GetString(nameof(RequiredError), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RequiredError);

    public static string EmailError =>
        _resourceManager.GetString(nameof(EmailError), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EmailError);
}
