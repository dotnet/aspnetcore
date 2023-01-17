// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Value-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
/// </summary>
public static class HtmlHelperValueExtensions
{
    /// <summary>
    /// Returns the formatted value for the specified <paramref name="expression"/>. Specifically, returns the
    /// first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A <see cref="string"/> containing the formatted value.</returns>
    /// <remarks>
    /// Converts the expression result to a <see cref="string"/> directly.
    /// </remarks>
    public static string Value(this IHtmlHelper htmlHelper, string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Value(expression, format: null);
    }

    /// <summary>
    /// Returns the formatted value for the specified <paramref name="expression"/>. Specifically, returns the
    /// first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A <see cref="string"/> containing the formatted value.</returns>
    /// <remarks>
    /// Converts the <paramref name="expression"/> result to a <see cref="string"/> directly.
    /// </remarks>
    public static string ValueFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.ValueFor(expression, format: null);
    }

    /// <summary>
    /// Returns the formatted value for the current model. Specifically, returns the
    /// first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <returns>A <see cref="string"/> containing the formatted value.</returns>
    /// <remarks>
    /// Converts the model value to a <see cref="string"/> directly.
    /// </remarks>
    public static string ValueForModel(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Value(expression: null, format: null);
    }

    /// <summary>
    /// Returns the formatted value for the current model. Specifically, returns the
    /// first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the return
    /// value unless that came from model binding.
    /// </param>
    /// <returns>A <see cref="string"/> containing the formatted value.</returns>
    /// <remarks>
    /// Converts the model value to a <see cref="string"/> directly if
    /// <paramref name="format"/> is <c>null</c> or empty.
    /// </remarks>
    public static string ValueForModel(this IHtmlHelper htmlHelper, string format)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Value(expression: null, format: format);
    }
}
