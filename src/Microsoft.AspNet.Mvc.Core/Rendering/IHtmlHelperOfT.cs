// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// An <see cref="IHtmlHelper"/> for Linq expressions.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public interface IHtmlHelper<TModel> : IHtmlHelper
    {
        /// <summary>
        /// Gets the current view data.
        /// </summary>
        new ViewDataDictionary<TModel> ViewData { get; }

        /// <summary>
        /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
        /// "hidden" with value "false".
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the checkbox element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
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
        /// <item>Existing "checked" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the <paramref name="htmlAttributes"/> case, includes a "checked" attribute with value "checked"
        /// if the <see cref="bool"/> values is <c>true</c>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        HtmlString CheckBoxFor([NotNull] Expression<Func<TModel, bool>> expression, object htmlAttributes);

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template, specified HTML field
        /// name, and additional view data. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for properties
        /// that have the same name.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="IDictionary{string, object}"/> that can contain additional
        /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
        /// template.
        /// </param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// <paramref name="expression"/> result.
        /// </remarks>
        HtmlString DisplayFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData);

        /// <summary>
        /// Returns the display name for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="string"/> containing the display name.</returns>
        string DisplayNameFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression);

        /// <summary>
        /// Returns the display name for the specified <paramref name="expression"/>
        /// if the current model represents a collection.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
        /// <typeparam name="TModelItem">The type of items in the model collection.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="string"/> containing the display name.</returns>
        string DisplayNameForInnerType<TModelItem, TResult>(
            [NotNull] Expression<Func<TModelItem, TResult>> expression);

        /// <summary>
        /// Returns the simple display text for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>
        /// A <see cref="string"/> containing the simple display text.
        /// If the <paramref name="expression"/> result is <c>null</c>, returns
        /// <see cref="ModelBinding.ModelMetadata.NullDisplayText"/>.
        /// </returns>
        string DisplayTextFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression);

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items, option label, and HTML attributes.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        HtmlString DropDownListFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel,
            object htmlAttributes);

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template, specified HTML field
        /// name, and additional view data. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template that is used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for properties
        /// that have the same name.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="IDictionary{string, object}"/> that can contain additional
        /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
        /// template.
        /// </param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the <paramref name="expression"/> result.
        /// </remarks>
        HtmlString EditorFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
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
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        HtmlString HiddenFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object htmlAttributes);

        /// <summary>
        /// Returns the HTML element Id for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="string"/> containing the element Id.</returns>
        string IdFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression);

        /// <summary>
        /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="labelText">The inner text of the element.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        HtmlString LabelFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string labelText,
            object htmlAttributes);

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items and HTML attributes.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
        /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
        /// representation of the <paramref name="expression"/> to set element's "id" attribute.
        /// </remarks>
        HtmlString ListBoxFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes);

        /// <summary>
        /// Returns the full HTML element name for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="string"/> containing the element name.</returns>
        string NameFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
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
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        HtmlString PasswordFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object htmlAttributes);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="value">Value to include in the element. Must not be <c>null</c>.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
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
        /// <item>Existing "checked" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the <paramref name="htmlAttributes"/> and default cases, includes a "checked" attribute with
        /// value "checked" if the <see cref="string"/> values is equal to a converted <see cref="string"/> for
        /// <paramref name="value"/>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        HtmlString RadioButtonFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            [NotNull] object value,
            object htmlAttributes);

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="rows">Number of rows in the textarea.</param>
        /// <param name="columns">Number of columns in the textarea.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
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
        HtmlString TextAreaFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            int rows,
            int columns,
            object htmlAttributes);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
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
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        HtmlString TextBoxFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string format,
            object htmlAttributes);

        /// <summary>
        /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
        /// object for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="message">
        /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
        /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
        /// validation may update the associated CSS class.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the <paramref name="tag"/> element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <param name="tag">
        /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
        /// <see cref="ViewContext.ValidationMessageElement"/>.
        /// </param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>
        /// A new <see cref="HtmlString"/> containing the <paramref name="tag"/> element. <c>null</c> if the
        /// <paramref name="expression"/> is valid and client-side validation is disabled.
        /// </returns>
        HtmlString ValidationMessageFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string message,
            object htmlAttributes,
            string tag);

        /// <summary>
        /// Returns the formatted value for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="string"/> containing the formatted value.</returns>
        /// <remarks>
        /// Converts the <paramref name="expression"/> result to a <see cref="string"/> directly if
        /// <paramref name="format"/> is <c>null</c> or empty.
        /// </remarks>
        string ValueFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string format);
    }
}
