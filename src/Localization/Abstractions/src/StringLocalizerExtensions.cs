// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Extension methods for operating on <see cref="IStringLocalizer" /> instances.
/// </summary>
public static class StringLocalizerExtensions
{
    /// <summary>
    /// Gets the string resource with the given name.
    /// </summary>
    /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
    /// <param name="name">The name of the string resource.</param>
    /// <returns>The string resource as a <see cref="LocalizedString"/>.</returns>
    public static LocalizedString GetString(
        this IStringLocalizer stringLocalizer,
        string name)
    {
        ArgumentNullThrowHelper.ThrowIfNull(stringLocalizer);
        ArgumentNullThrowHelper.ThrowIfNull(name);

        return stringLocalizer[name];
    }

    /// <summary>
    /// Gets the string resource with the given name and formatted with the supplied arguments.
    /// </summary>
    /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="arguments">The values to format the string with.</param>
    /// <returns>The formatted string resource as a <see cref="LocalizedString"/>.</returns>
    public static LocalizedString GetString(
        this IStringLocalizer stringLocalizer,
        string name,
        params object[] arguments)
    {
        ArgumentNullThrowHelper.ThrowIfNull(stringLocalizer);
        ArgumentNullThrowHelper.ThrowIfNull(name);

        return stringLocalizer[name, arguments];
    }

    /// <summary>
    /// Gets all string resources including those for parent cultures.
    /// </summary>
    /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
    /// <returns>The string resources.</returns>
    public static IEnumerable<LocalizedString> GetAllStrings(this IStringLocalizer stringLocalizer)
    {
        ArgumentNullThrowHelper.ThrowIfNull(stringLocalizer);

        return stringLocalizer.GetAllStrings(includeParentCultures: true);
    }
}
