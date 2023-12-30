// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Input-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
/// </summary>
public static class HtmlHelperInputExtensions
{
    /// <summary>
    /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
    /// "hidden" with value "false" for the specified <paramref name="expression"/>. Adds a "checked" attribute to
    /// the "checkbox" element based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; elements.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// checkbox element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent CheckBox(this IHtmlHelper htmlHelper, string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.CheckBox(expression, isChecked: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
    /// "hidden" with value "false" for the specified <paramref name="expression"/>. Adds a "checked" attribute to
    /// the "checkbox" element based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="isChecked"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="isChecked">If <c>true</c>, checkbox is initially checked.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; elements.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// checkbox element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent CheckBox(
        this IHtmlHelper htmlHelper,
        string expression,
        bool isChecked)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.CheckBox(expression, isChecked, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
    /// "hidden" with value "false" for the specified <paramref name="expression"/>. Adds a "checked" attribute to
    /// the "checkbox" element based on the first non-<c>null</c> value found in:
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "checked",
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the checkbox element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; elements.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// checkbox element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent CheckBox(
        this IHtmlHelper htmlHelper,
        string expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.CheckBox(expression, isChecked: null, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
    /// "hidden" with value "false" for the specified <paramref name="expression"/>. Adds a "checked" attribute to
    /// the "checkbox" element based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; elements.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set checkbox element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set checkbox element's "id" attribute.
    /// </remarks>
    public static IHtmlContent CheckBoxFor<TModel>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.CheckBoxFor(expression, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent Hidden(this IHtmlHelper htmlHelper, string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Hidden(expression, value: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent Hidden(
        this IHtmlHelper htmlHelper,
        string expression,
        object value)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Hidden(expression, value, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    public static IHtmlContent HiddenFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.HiddenFor(expression, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>. Does
    /// not add a "value" attribute.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute. Sets &lt;input&gt; element's "value" attribute to <c>string.Empty</c>.
    /// </remarks>
    public static IHtmlContent Password(this IHtmlHelper htmlHelper, string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Password(expression, value: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute containing the <paramref name="value"/> parameter if that is non-<c>null</c>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent Password(
        this IHtmlHelper htmlHelper,
        string expression,
        object value)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Password(expression, value, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>. Does
    /// not add a "value" attribute.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    public static IHtmlContent PasswordFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.PasswordFor(expression, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
    /// Adds a "value" attribute to the element containing the <paramref name="value"/> parameter if that is
    /// non-<c>null</c>.
    /// Adds a "checked" attribute to the element if <paramref name="value"/> matches the first non-<c>null</c>
    /// value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">Value to include in the element. Must not be <c>null</c>.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute. Sets &lt;input&gt; element's "value" attribute to <paramref name="value"/>.
    /// </remarks>
    public static IHtmlContent RadioButton(
        this IHtmlHelper htmlHelper,
        string expression,
        object value)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.RadioButton(expression, value, isChecked: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
    /// Adds a "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <paramref name="value"/> parameter, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// Adds a "checked" attribute to the element if <paramref name="value"/> matches the first non-<c>null</c>
    /// value found in:
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "checked",
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">
    /// If non-<c>null</c>, value to include in the element. Must not be <c>null</c> if no "checked" entry exists
    /// in <paramref name="htmlAttributes"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent RadioButton(
        this IHtmlHelper htmlHelper,
        string expression,
        object value,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.RadioButton(expression, value, isChecked: null, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
    /// Adds a "value" attribute to the element containing the <paramref name="value"/> parameter if that is
    /// non-<c>null</c>.
    /// Adds a "checked" attribute to the element if <paramref name="value"/> matches the first non-<c>null</c>
    /// value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="isChecked"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent RadioButton(
        this IHtmlHelper htmlHelper,
        string expression,
        object value,
        bool isChecked)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.RadioButton(expression, value, isChecked, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
    /// Adds a "value" attribute to the element containing the <paramref name="value"/> parameter.
    /// Adds a "checked" attribute to the element if <paramref name="value"/> matches the first non-<c>null</c>
    /// value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="value">Value to include in the element. Must not be <c>null</c>.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute. Converts the
    /// <paramref name="value"/> to a <see cref="string"/> to set element's "value" attribute.
    /// </remarks>
    public static IHtmlContent RadioButtonFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        object value)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(value);

        return htmlHelper.RadioButtonFor(expression, value, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextBox(this IHtmlHelper htmlHelper, string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextBox(expression, value: null, format: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextBox(
        this IHtmlHelper htmlHelper,
        string expression,
        object value)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextBox(expression, value, format: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the "value"
    /// attribute unless that came from model binding.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextBox(
        this IHtmlHelper htmlHelper,
        string expression,
        object value,
        string format)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextBox(expression, value, format, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name,
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextBox(
        this IHtmlHelper htmlHelper,
        string expression,
        object value,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextBox(expression, value, format: null, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    public static IHtmlContent TextBoxFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.TextBoxFor(expression, format: null, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the
    /// <paramref name="expression"/> value when using that in the "value" attribute.
    /// </param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    public static IHtmlContent TextBoxFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string format)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.TextBoxFor(expression, format, htmlAttributes: null);
    }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    public static IHtmlContent TextBoxFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.TextBoxFor(expression, format: null, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextArea(
        this IHtmlHelper htmlHelper,
        string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextArea(expression, value: null, rows: 0, columns: 0, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextArea(
        this IHtmlHelper htmlHelper,
        string expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextArea(expression, value: null, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextArea(
        this IHtmlHelper htmlHelper,
        string expression,
        string value)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextArea(expression, value, rows: 0, columns: 0, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="IHtmlHelper.ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper.Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    public static IHtmlContent TextArea(
        this IHtmlHelper htmlHelper,
        string expression,
        string value,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.TextArea(expression, value, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;textarea&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    public static IHtmlContent TextAreaFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.TextAreaFor(expression, rows: 0, columns: 0, htmlAttributes: null);
    }

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewFeatures.ViewDataDictionary.Model"/>.
    /// See <see cref="IHtmlHelper{TModel}.NameFor"/> for more information about a "full name".
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="ViewFeatures.TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;textarea&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    public static IHtmlContent TextAreaFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.TextAreaFor(expression, rows: 0, columns: 0, htmlAttributes: htmlAttributes);
    }
}
