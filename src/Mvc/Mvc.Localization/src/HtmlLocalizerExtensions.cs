// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// Extension methods for <see cref="IHtmlLocalizer"/>.
/// </summary>
public static class HtmlLocalizerExtensions
{
    /// <summary>
    /// Gets the <see cref="LocalizedHtmlString"/> resource for a specific name.
    /// </summary>
    /// <param name="htmlLocalizer">The <see cref="IHtmlLocalizer"/>.</param>
    /// <param name="name">The key to use.</param>
    /// <returns>The <see cref="LocalizedHtmlString"/> resource.</returns>
    public static LocalizedHtmlString GetHtml(this IHtmlLocalizer htmlLocalizer, string name)
    {
        ArgumentNullException.ThrowIfNull(htmlLocalizer);
        ArgumentNullException.ThrowIfNull(name);

        return htmlLocalizer[name];
    }

    /// <summary>
    /// Gets the <see cref="LocalizedHtmlString"/> resource for a specific name.
    /// </summary>
    /// <param name="htmlLocalizer">The <see cref="IHtmlLocalizer"/>.</param>
    /// <param name="name">The key to use.</param>
    /// <param name="arguments">The values to format the string with.</param>
    /// <returns>The <see cref="LocalizedHtmlString"/> resource.</returns>
    public static LocalizedHtmlString GetHtml(this IHtmlLocalizer htmlLocalizer, string name, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(htmlLocalizer);
        ArgumentNullException.ThrowIfNull(name);

        return htmlLocalizer[name, arguments];
    }

    /// <summary>
    /// Gets all string resources including those for parent cultures.
    /// </summary>
    /// <param name="htmlLocalizer">The <see cref="IHtmlLocalizer"/>.</param>
    /// <returns>The string resources.</returns>
    public static IEnumerable<LocalizedString> GetAllStrings(this IHtmlLocalizer htmlLocalizer)
    {
        ArgumentNullException.ThrowIfNull(htmlLocalizer);

        return htmlLocalizer.GetAllStrings(includeParentCultures: true);
    }
}
