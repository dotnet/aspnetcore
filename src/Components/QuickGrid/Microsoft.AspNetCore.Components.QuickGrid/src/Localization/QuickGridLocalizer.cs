// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Simple, public customization point for QuickGrid localization.
/// Applications can override this via DI to provide custom translations.
/// </summary>
public class QuickGridLocalizer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuickGridLocalizer"/> class.
    /// Applications may derive from this type and register their implementation via DI.
    /// </summary>
    public QuickGridLocalizer()
    {
    }

    /// <summary>
    /// Gets the localized entry for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>A <see cref="Microsoft.Extensions.Localization.LocalizedString"/> where <see cref="Microsoft.Extensions.Localization.LocalizedString.ResourceNotFound"/> indicates missing translation.</returns>
    public virtual Microsoft.Extensions.Localization.LocalizedString this[string key]
    {
        get => new Microsoft.Extensions.Localization.LocalizedString(key, key, resourceNotFound: true);
    }

    /// <summary>
    /// Gets the localized entry for the specified <paramref name="key"/> and formats it with <paramref name="arguments"/>.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="arguments">Format arguments, if any.</param>
    /// <returns>A formatted <see cref="Microsoft.Extensions.Localization.LocalizedString"/>.</returns>
    public virtual Microsoft.Extensions.Localization.LocalizedString this[string key, params object?[]? arguments]
    {
        get
        {
            var localized = this[key];
            if (arguments == null || arguments.Length == 0)
            {
                return localized;
            }

            string formatted;
            try
            {
                formatted = string.Format(System.Globalization.CultureInfo.CurrentCulture, localized.Value, arguments);
            }
            catch
            {
                formatted = localized.Value;
            }

            return new Microsoft.Extensions.Localization.LocalizedString(localized.Name, formatted, localized.ResourceNotFound);
        }
    }
}
