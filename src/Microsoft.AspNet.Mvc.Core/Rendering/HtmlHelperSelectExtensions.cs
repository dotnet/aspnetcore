// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Select-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperSelectExtensions
    {
        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.DropDownList(expression, selectList: null, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// option label.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string optionLabel)
        {
            return htmlHelper.DropDownList(
                expression,
                selectList: null,
                optionLabel: optionLabel,
                htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.DropDownList(expression, selectList, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items and HTML attributes.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return htmlHelper.DropDownList(expression, selectList, optionLabel: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items and option label.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            return htmlHelper.DropDownList(expression, selectList, optionLabel, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString DropDownListFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items and HTML attributes.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString DropDownListFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return htmlHelper.DropDownListFor(
                expression,
                selectList,
                optionLabel: null,
                htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items and option label.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString DropDownListFor<TModel, TResult>([
            NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString ListBox([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.ListBox(expression, selectList: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString ListBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.ListBox(expression, selectList, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the  <paramref name="expression"/>, using the
        /// specified list items.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString ListBoxFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.ListBoxFor(expression, selectList, htmlAttributes: null);
        }
    }
}
