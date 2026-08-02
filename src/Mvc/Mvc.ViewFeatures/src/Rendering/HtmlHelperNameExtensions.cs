// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Name-related extensions for <see cref="IHtmlHelper"/>.
/// </summary>
public static class HtmlHelperNameExtensions
{
    /// <summary>
    /// Returns the full HTML element name for the current model. Uses
    /// <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> (if non-empty) to reflect relationship between
    /// current <see cref="ViewFeatures.ViewDataDictionary.Model"/> and the top-level view's model.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <returns>A <see cref="string"/> containing the element name.</returns>
    public static string NameForModel(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Name(expression: null);
    }

    /// <summary>
    /// Returns the HTML element Id for the current model.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <returns>A <see cref="string"/> containing the element Id.</returns>
    public static string IdForModel(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Id(expression: null);
    }
}
