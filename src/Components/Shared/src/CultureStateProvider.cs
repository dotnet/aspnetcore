// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;

namespace Microsoft.AspNetCore.Components;

internal class CultureStateProvider
{
    [PersistentState]
    public string? CurrentCultureName { get; set; }

    [PersistentState]
    public string? CurrentUICultureName { get; set; }

    public void CaptureCurrentCulture()
    {
        CurrentCultureName = CultureInfo.CurrentCulture.Name;
        CurrentUICultureName = CultureInfo.CurrentUICulture.Name;
    }

    private static CultureInfo? TryGetCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    public void ApplyStoredCulture()
    {
        var currentCultureName = CultureInfo.CurrentCulture.Name;
        var currentUICultureName = CultureInfo.CurrentUICulture.Name;

        if (!string.IsNullOrEmpty(CurrentCultureName) &&
            currentCultureName != CurrentCultureName &&
            TryGetCulture(CurrentCultureName) is { } culture)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
        }

        if (!string.IsNullOrEmpty(CurrentUICultureName) &&
            currentUICultureName != CurrentUICultureName &&
            TryGetCulture(CurrentUICultureName) is { } cultureUI)
        {
            CultureInfo.CurrentUICulture = cultureUI;
            CultureInfo.DefaultThreadCurrentUICulture = cultureUI;
        }
    }
}
