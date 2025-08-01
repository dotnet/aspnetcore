// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms;

internal class CultureStateProvider
{
    protected string? _currentCultureName;
    protected string? _currentUICultureName;

    [PersistentState]
    public string? CurrentCultureName
    {
        get => _currentCultureName;
        set => _currentCultureName = value;
    }

    [PersistentState]
    public string? CurrentUICultureName
    {
        get => _currentUICultureName;
        set => _currentUICultureName = value;
    }

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
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(CurrentCultureName);
            }
            catch (CultureNotFoundException ex)
            {
                Console.WriteLine($"CultureStateProvider: Warning - Could not apply culture '{CurrentCultureName}': {ex.Message}");
            }
        }
        
        if (!string.IsNullOrEmpty(CurrentUICultureName))
        {
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(CurrentUICultureName);
            }
            catch (CultureNotFoundException ex)
            {
                Console.WriteLine($"CultureStateProvider: Warning - Could not apply UI culture '{CurrentUICultureName}': {ex.Message}");
            }
        }
    }
}
