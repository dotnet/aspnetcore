// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms;

internal class CultureStateProvider
{

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
            try
            {
                var culture = CultureInfo.GetCultureInfo(CurrentCultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
            }
            catch (CultureNotFoundException)
            {
                // The server may have culture data that isn't available on the client
                // (e.g., invariant globalization or a sharded ICU subset).
            }
        }

        if (!string.IsNullOrEmpty(CurrentUICultureName))
        {
            try
            {
                var uiCulture = CultureInfo.GetCultureInfo(CurrentUICultureName);
                CultureInfo.CurrentUICulture = uiCulture;
                CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
            }
            catch (CultureNotFoundException)
            {
                // The server may have culture data that isn't available on the client
                // (e.g., invariant globalization or a sharded ICU subset).
            }
        }
    }
}
