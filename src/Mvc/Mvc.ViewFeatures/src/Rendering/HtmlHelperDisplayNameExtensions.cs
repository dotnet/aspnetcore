// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// DisplayName-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
/// </summary>
public static class HtmlHelperDisplayNameExtensions
{
    /// <summary>
    /// Returns the display name for the current model.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <returns>A <see cref="string"/> containing the display name.</returns>
    public static string DisplayNameForModel(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.DisplayName(expression: null);
    }

    /// <summary>
    /// Returns the display name for the specified <paramref name="expression"/>
    /// if the current model represents a collection.
    /// </summary>
    /// <param name="htmlHelper">
    /// The <see cref="IHtmlHelper{T}"/> of <see cref="IEnumerable{TModelItem}"/> instance this method extends.
    /// </param>
    /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
    /// <typeparam name="TModelItem">The type of items in the model collection.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A <see cref="string"/> containing the display name.</returns>
    public static string DisplayNameFor<TModelItem, TResult>(
        this IHtmlHelper<IEnumerable<TModelItem>> htmlHelper,
        Expression<Func<TModelItem, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.DisplayNameForInnerType(expression);
    }
}
