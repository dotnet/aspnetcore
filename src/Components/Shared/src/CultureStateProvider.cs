// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms;

internal class CultureStateProvider
{
    protected string? _currentCultureName;
    protected string? _currentUICultureName;

    [PersistentState]
    public string? CurrentCultureName { get; set; }

    [PersistentState]
    public string? CurrentUICultureName { get; set; }

    /// <summary>
    /// Captures the current thread culture for persistence.
    /// Called from server-side during prerendering.
    /// </summary>
    public void CaptureCurrentCulture()
    {
        CurrentCultureName = CultureInfo.CurrentCulture.Name;
        CurrentUICultureName = CultureInfo.CurrentUICulture.Name;
    }

    /// <summary>
    /// Applies the stored culture to the current thread.
    /// Called on WebAssembly side after hydration.
    /// </summary>
    public void ApplyStoredCulture()
    {
        if (!string.IsNullOrEmpty(CurrentCultureName))
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(CurrentCultureName);
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(CurrentCultureName);
        }

        if (!string.IsNullOrEmpty(CurrentUICultureName))
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(CurrentUICultureName);
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(CurrentUICultureName);
        }
    }
}
