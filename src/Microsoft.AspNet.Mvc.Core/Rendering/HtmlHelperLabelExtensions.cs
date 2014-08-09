// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Label-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperLabelExtensions
    {
        /// <summary>
        /// Returns a &lt;label&gt; element for the specified expression <paramref name="name"/>.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString Label([NotNull] this IHtmlHelper html, string expression)
        {
            return html.Label(expression,
                             labelText: null,
                             htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified expression <paramref name="name"/>.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString Label([NotNull] this IHtmlHelper html, string expression, string labelText)
        {
            return html.Label(expression, labelText, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                          [NotNull] Expression<Func<TModel, TValue>> expression)
        {
            return html.LabelFor<TValue>(expression, labelText: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                          [NotNull] Expression<Func<TModel, TValue>> expression,
                                                          string labelText)
        {
            return html.LabelFor<TValue>(expression, labelText, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                          [NotNull] Expression<Func<TModel, TValue>> expression,
                                                          object htmlAttributes)
        {
            return html.LabelFor<TValue>(expression, labelText: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel([NotNull] this IHtmlHelper html)
        {
            return html.Label(expression: null, labelText: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel([NotNull] this IHtmlHelper html, string labelText)
        {
            return html.Label(expression: null, labelText: labelText, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel([NotNull] this IHtmlHelper html, object htmlAttributes)
        {
            return html.Label(expression: null, labelText: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a &lt;label&gt; element for the current model.
        /// </summary>
        /// <param name="html">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        public static HtmlString LabelForModel(
            [NotNull] this IHtmlHelper html,
            string labelText,
            object htmlAttributes)
        {
            return html.Label(expression: null, labelText: labelText, htmlAttributes: htmlAttributes);
        }
    }
}