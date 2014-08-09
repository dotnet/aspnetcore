// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Select-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperSelectExtensions
    {
        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the expression <paramref name="name"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="name"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="name"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.DropDownList(name, selectList: null, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the expression <paramref name="name"/>,
        /// using the option label.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="name"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="name"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList([NotNull] this IHtmlHelper htmlHelper, string name, string optionLabel)
        {
            return htmlHelper.DropDownList(name, selectList: null, optionLabel: optionLabel, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the expression <paramref name="name"/>,
        /// using the specified list items.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="name"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="name"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the expression <paramref name="name"/>,
        /// using the specified list items and HTML attributes.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
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
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="name"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="name"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the expression <paramref name="name"/>,
        /// using the specified list items and option label.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="name"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="name"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString DropDownList(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel, htmlAttributes: null);
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
        /// <typeparam name="TProperty">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
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
        /// <typeparam name="TProperty">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel: null,
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
        /// <typeparam name="TProperty">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the expression <paramref name="name"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="name"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="name"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString ListBox([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.ListBox(name, selectList: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the expression <paramref name="name"/>, using the
        /// specified list items.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="name">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="name"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="name"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static HtmlString ListBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string name,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.ListBox(name, selectList, htmlAttributes: null);
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
        /// <typeparam name="TProperty">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static HtmlString ListBoxFor<TModel, TProperty>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.ListBoxFor(expression, selectList, htmlAttributes: null);
        }
    }
}
