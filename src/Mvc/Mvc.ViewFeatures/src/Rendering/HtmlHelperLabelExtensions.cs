// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Label-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
/// </summary>
public static class HtmlHelperLabelExtensions
{
    /// <summary>
    /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent Label(this IHtmlHelper htmlHelper, string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Label(expression, labelText: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="labelText">The inner text of the element.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent Label(this IHtmlHelper htmlHelper, string expression, string labelText)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Label(expression, labelText, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent LabelFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.LabelFor(expression, labelText: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="labelText">The inner text of the element.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent LabelFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string labelText)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.LabelFor<TResult>(expression, labelText, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent LabelFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.LabelFor<TResult>(expression, labelText: null, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the current model.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent LabelForModel(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Label(expression: null, labelText: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the current model.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="labelText">The inner text of the element.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent LabelForModel(this IHtmlHelper htmlHelper, string labelText)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Label(expression: null, labelText: labelText, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the current model.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent LabelForModel(this IHtmlHelper htmlHelper, object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Label(expression: null, labelText: null, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns a &lt;label&gt; element for the current model.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="labelText">The inner text of the element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    public static IHtmlContent LabelForModel(
        this IHtmlHelper htmlHelper,
        string labelText,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Label(expression: null, labelText: labelText, htmlAttributes: htmlAttributes);
    }
}
