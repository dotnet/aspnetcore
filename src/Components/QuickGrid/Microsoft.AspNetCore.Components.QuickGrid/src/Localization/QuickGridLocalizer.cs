// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Simple, public customization point for QuickGrid localization.
/// </summary>
internal class QuickGridLocalizer
{
    public virtual QuickGridLocalizedString this[string key]
    {
        get => new QuickGridLocalizedString(key, key, resourceNotFound: true);
    }

    public virtual QuickGridLocalizedString this[string key, params object?[]? arguments]
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

            return new QuickGridLocalizedString(localized.Name, formatted, localized.ResourceNotFound);
        }
    }
}
