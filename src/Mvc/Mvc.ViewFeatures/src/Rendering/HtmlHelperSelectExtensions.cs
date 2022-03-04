// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Select-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperSelectExtensions
    {
        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on the <see cref="IHtmlHelper.ViewData"/> entry with full name. Adds a
        /// "selected" attribute to an &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name, or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>
        /// The <see cref="IHtmlHelper.ViewData"/> entry with full name must be a non-<c>null</c> collection of
        /// <see cref="SelectListItem"/> objects.
        /// </para>
        /// </remarks>
        public static IHtmlContent DropDownList(this IHtmlHelper htmlHelper, string expression)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.DropDownList(expression, selectList: null, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="optionLabel"/> and the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name. Adds a "selected" attribute to an &lt;option&gt; if its
        /// <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or <see cref="SelectListItem.Text"/> matches the
        /// first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name, or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>
        /// The <see cref="IHtmlHelper.ViewData"/> entry with full name must be a non-<c>null</c> collection of
        /// <see cref="SelectListItem"/> objects.
        /// </para>
        /// </remarks>
        public static IHtmlContent DropDownList(
            this IHtmlHelper htmlHelper,
            string expression,
            string optionLabel)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.DropDownList(
                expression,
                selectList: null,
                optionLabel: optionLabel,
                htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
        /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name,
        /// the <see cref="IHtmlHelper.ViewData"/> entry with full name (unless used instead of
        /// <paramref name="selectList"/>), or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static IHtmlContent DropDownList(
            this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.DropDownList(expression, selectList, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
        /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name,
        /// the <see cref="IHtmlHelper.ViewData"/> entry with full name (unless used instead of
        /// <paramref name="selectList"/>), or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static IHtmlContent DropDownList(
            this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.DropDownList(expression, selectList, optionLabel: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="optionLabel"/> and <paramref name="selectList"/>. Adds a
        /// "selected" attribute to an &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name,
        /// the <see cref="IHtmlHelper.ViewData"/> entry with full name (unless used instead of
        /// <paramref name="selectList"/>), or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static IHtmlContent DropDownList(
            this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.DropDownList(expression, selectList, optionLabel, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
        /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name, or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static IHtmlContent DropDownListFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.DropDownListFor(expression, selectList, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
        /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name, or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static IHtmlContent DropDownListFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.DropDownListFor(
                expression,
                selectList,
                optionLabel: null,
                htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="optionLabel"/> and <paramref name="selectList"/>. Adds a
        /// "selected" attribute to an &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name, or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static IHtmlContent DropDownListFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.DropDownListFor(expression, selectList, optionLabel, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on the <see cref="IHtmlHelper.ViewData"/> entry with full name. Adds a
        /// "selected" attribute to an &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name, or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>
        /// The <see cref="IHtmlHelper.ViewData"/> entry with full name must be a non-<c>null</c> collection of
        /// <see cref="SelectListItem"/> objects.
        /// </para>
        /// </remarks>
        public static IHtmlContent ListBox(this IHtmlHelper htmlHelper, string expression)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.ListBox(expression, selectList: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
        /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name,
        /// the <see cref="IHtmlHelper.ViewData"/> entry with full name (unless used instead of
        /// <paramref name="selectList"/>), or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        public static IHtmlContent ListBox(
            this IHtmlHelper htmlHelper,
            string expression,
            IEnumerable<SelectListItem> selectList)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.ListBox(expression, selectList, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the  <paramref name="expression"/>. Adds
        /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
        /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
        /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
        /// the <see cref="ActionContext.ModelState"/> entry with full name, or
        /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
        /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="IHtmlHelper.ViewData"/>
        /// entry with full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        public static IHtmlContent ListBoxFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.ListBoxFor(expression, selectList, htmlAttributes: null);
        }
    }
}
