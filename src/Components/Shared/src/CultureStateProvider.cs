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

    public void ApplyStoredCulture()
    {
        var currentCultureName = CultureInfo.CurrentCulture.Name;
        var currentUICultureName = CultureInfo.CurrentUICulture.Name;

        if (!string.IsNullOrEmpty(CurrentCultureName) && currentCultureName != CurrentCultureName)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(CurrentCultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
            }
            catch (CultureNotFoundException)
            {

            }
        }

        if (!string.IsNullOrEmpty(CurrentUICultureName) && currentUICultureName != CurrentUICultureName)
        {
            try
            {
                var uiCulture = CultureInfo.GetCultureInfo(CurrentUICultureName);
                CultureInfo.CurrentUICulture = uiCulture;
                CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
            }
            catch (CultureNotFoundException)
            {

            }
        }
    }
}
