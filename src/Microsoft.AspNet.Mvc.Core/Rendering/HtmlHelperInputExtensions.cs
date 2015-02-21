// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Input-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperInputExtensions
    {
        /// <summary>
        /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
        /// "hidden" with value "false".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; elements.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set checkbox
        /// element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines checkbox element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="bool"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the default case, includes a "checked" attribute with
        /// value "checked" if the <see cref="bool"/> values is <c>true</c>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString CheckBox([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.CheckBox(expression, isChecked: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
        /// "hidden" with value "false".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="isChecked">If <c>true</c>, checkbox is initially checked.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; elements.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set checkbox
        /// element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines checkbox element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item><paramref name="isChecked"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="bool"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the default case, includes a "checked" attribute with
        /// value "checked" if the <see cref="bool"/> values is <c>true</c>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString CheckBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            bool isChecked)
        {
            return htmlHelper.CheckBox(expression, isChecked, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
        /// "hidden" with value "false".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the checkbox element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; elements.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set checkbox
        /// element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines checkbox element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="bool"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Existing "checked" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the <paramref name="htmlAttributes"/> and default cases, includes a "checked" attribute with
        /// value "checked" if the <see cref="bool"/> values is <c>true</c>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString CheckBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object htmlAttributes)
        {
            return htmlHelper.CheckBox(expression, isChecked: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
        /// "hidden" with value "false".
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; elements.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set checkbox element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set checkbox element's "id" attribute.
        /// </para>
        /// <para>Determines checkbox element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a
        /// <see cref="bool"/>.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the default case, includes a "checked" attribute with
        /// value "checked" if the <see cref="bool"/> values is <c>true</c>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString CheckBoxFor<TModel>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, bool>> expression)
        {
            return htmlHelper.CheckBoxFor(expression, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString Hidden([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.Hidden(expression, value: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString Hidden(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value)
        {
            return htmlHelper.Hidden(expression, value, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a
        /// <see cref="string"/>.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString HiddenFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return htmlHelper.HiddenFor(expression, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute. Sets &lt;input&gt; element's "value" attribute to <c>string.Empty</c>.
        /// </remarks>
        public static HtmlString Password([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.Password(expression, value: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString Password(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value)
        {
            return htmlHelper.Password(expression, value, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a
        /// <see cref="string"/>.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString PasswordFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return htmlHelper.PasswordFor(expression, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">Value to include in the element. Must not be <c>null</c>.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute. Sets &lt;input&gt; element's "value" attribute to <paramref name="value"/>.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the default case, includes a "checked" attribute with
        /// value "checked" if the <see cref="string"/> values is equal to a converted <see cref="string"/> for
        /// <paramref name="value"/> or <paramref name="isChecked"/> is <c>true</c> (for that case); does not include
        /// the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString RadioButton(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value)
        {
            return htmlHelper.RadioButton(expression, value, isChecked: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">
        /// If non-<c>null</c>, value to include in the element. Must not be <c>null</c> if no "checked" entry exists
        /// in <paramref name="htmlAttributes"/>.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// <para>Determines &lt;input&gt; element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>Existing "checked" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the <paramref name="htmlAttributes"/> and default cases, includes a "checked" attribute with
        /// value "checked" if the <see cref="string"/> values is equal to a converted <see cref="string"/> for
        /// <paramref name="value"/> or <paramref name="isChecked"/> is <c>true</c> (for that case); does not include
        /// the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString RadioButton(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value,
            object htmlAttributes)
        {
            return htmlHelper.RadioButton(expression, value, isChecked: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">
        /// If non-<c>null</c>, value to include in the element. Must not be <c>null</c> if
        /// <paramref name="isChecked"/> is also <c>null</c>.
        /// </param>
        /// <param name="isChecked">
        /// If <c>true</c>, radio button is initially selected. Must not be <c>null</c> if
        /// <paramref name="value"/> is also <c>null</c>.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// <para>Determines &lt;input&gt; element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item><paramref name="isChecked"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the default case, includes a "checked" attribute with
        /// value "checked" if the <see cref="string"/> values is equal to a converted <see cref="string"/> for
        /// <paramref name="value"/> or <paramref name="isChecked"/> is <c>true</c> (for that case); does not include
        /// the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString RadioButton(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value,
            bool isChecked)
        {
            return htmlHelper.RadioButton(expression, value, isChecked, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="value">Value to include in the element. Must not be <c>null</c>.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute. Converts the
        /// <paramref name="value"/> to a <see cref="string"/> to set element's "value" attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a <see cref="string"/>.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the default case, includes a "checked" attribute with
        /// value "checked" if the <see cref="string"/> values is equal to a converted <see cref="string"/> for
        /// <paramref name="value"/>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        public static HtmlString RadioButtonFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            [NotNull] object value)
        {
            return htmlHelper.RadioButtonFor(expression, value, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextBox([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.TextBox(expression, value: null, format: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="value"/> if non-<c>null</c>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value)
        {
            return htmlHelper.TextBox(expression, value, format: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="value"/> if non-<c>null</c>. Formats <paramref name="value"/> using
        /// <paramref name="format"/> or converts <paramref name="value"/> to a <see cref="string"/> directly if
        /// <paramref name="format"/> is <c>null</c> or empty.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>. Formats entry using
        /// <paramref name="format"/> or converts entry to a <see cref="string"/> directly if <paramref name="format"/>
        /// is <c>null</c> or empty.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property. Formats result using <paramref name="format"/> or converts result to a <see cref="string"/>
        /// directly if <paramref name="format"/> is <c>null</c> or empty.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value,
            string format)
        {
            return htmlHelper.TextBox(expression, value, format, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="value"/> if non-<c>null</c>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextBox(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object value,
            object htmlAttributes)
        {
            return htmlHelper.TextBox(expression, value, format: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a <see cref="string"/>.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextBoxFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return htmlHelper.TextBoxFor(expression, format: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a <see cref="string"/>.
        /// Formats result using <paramref name="format"/> or converts result to a <see cref="string"/> directly if
        /// <paramref name="format"/> is <c>null</c> or empty.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextBoxFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string format)
        {
            return htmlHelper.TextBoxFor(expression, format, htmlAttributes: null);
        }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
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
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a <see cref="string"/>.
        /// </item>
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextBoxFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object htmlAttributes)
        {
            return htmlHelper.TextBoxFor(expression, format: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;textarea&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;textarea&gt; element's content based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextArea(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression)
        {
            return htmlHelper.TextArea(expression, value: null, rows: 0, columns: 0, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;textarea&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;textarea&gt; element's content based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextArea(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object htmlAttributes)
        {
            return htmlHelper.TextArea(expression, value: null, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;textarea&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;textarea&gt; element's content based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextArea(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string value)
        {
            return htmlHelper.TextArea(expression, value, rows: 0, columns: 0, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;textarea&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;textarea&gt; element's content based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a
        /// fully-qualified name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewDataDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextArea(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string value,
            object htmlAttributes)
        {
            return htmlHelper.TextArea(expression, value, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;textarea&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;textarea&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </para>
        /// <para>Determines &lt;textarea&gt; element's content based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a <see cref="string"/>.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextAreaFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return htmlHelper.TextAreaFor(expression, rows: 0, columns: 0, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
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
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;textarea&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;textarea&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </para>
        /// <para>Determines &lt;textarea&gt; element's content based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelBinding.ModelStateDictionary"/> entry for the string representation of the
        /// <paramref name="expression"/> if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="expression"/> result if it is non-<c>null</c> and can be parsed as a <see cref="string"/>.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        public static HtmlString TextAreaFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object htmlAttributes)
        {
            return htmlHelper.TextAreaFor(expression, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
        }
    }
}
