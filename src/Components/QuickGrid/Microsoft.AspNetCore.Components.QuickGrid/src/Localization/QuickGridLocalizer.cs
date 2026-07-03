// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Base localizer that applications can override to provide QuickGrid translations.
/// </summary>
/// <remarks>
/// Register a derived instance with the application's dependency injection container
/// to override the built-in (English) strings used by QuickGrid. Returning a
/// <see cref="LocalizedString"/> with <c>ResourceNotFound</c> set to <c>true</c> falls
/// back to the default English resource shipped with QuickGrid. The chosen culture
/// is the thread's <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>;
/// the host application is responsible for switching cultures (for example via
/// <c>?culture=fr-FR</c> in the E2E test host).
/// </remarks>
public class QuickGridLocalizer
{
    /// <summary>
    /// Gets the localized string for the specified key.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <returns>
    /// The localized string. Return a <see cref="LocalizedString"/> with
    /// <c>ResourceNotFound</c> set to <c>true</c> to opt out of overriding that key
    /// and use the default English resource.
    /// </returns>
    public virtual LocalizedString this[string key] => new(key, key, resourceNotFound: true);

    /// <summary>
    /// Gets the localized string for the specified key and formats it with the supplied arguments.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="arguments">Arguments used to format the localized string.</param>
    /// <returns>The localized and formatted string.</returns>
    public virtual LocalizedString this[string key, params object[] arguments]
    {
        get
        {
            arguments ??= Array.Empty<object>();

            var localizedString = this[key];

            if (arguments.Length == 0)
            {
                return localizedString;
            }

            var formattedValue = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                localizedString.Value,
                arguments);

            return new LocalizedString(
                localizedString.Name,
                formattedValue,
                localizedString.ResourceNotFound);
        }
    }
}
