// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
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
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString Label([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.Label(expression, labelText: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString Label([NotNull] this IHtmlHelper htmlHelper, string expression, string labelText)
        {
            return htmlHelper.Label(expression, labelText, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return htmlHelper.LabelFor<TResult>(expression, labelText: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string labelText)
        {
            return htmlHelper.LabelFor<TResult>(expression, labelText, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object htmlAttributes)
        {
            return htmlHelper.LabelFor<TResult>(expression, labelText: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Label(expression: null, labelText: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel([NotNull] this IHtmlHelper htmlHelper, string labelText)
        {
            return htmlHelper.Label(expression: null, labelText: labelText, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel([NotNull] this IHtmlHelper htmlHelper, object htmlAttributes)
        {
            return htmlHelper.Label(expression: null, labelText: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel(
            [NotNull] this IHtmlHelper htmlHelper,
            string labelText,
            object htmlAttributes)
        {
            return htmlHelper.Label(expression: null, labelText: labelText, htmlAttributes: htmlAttributes);
        }
    }
}